using MyTrailerSystem.Models;
using System.Text.Json;

namespace MyTrailerSystem.Services
{
    public interface IBookingService
    {
        Task<List<Booking>> GetAllBookingsAsync();
        Task<List<Booking>> GetCustomerBookingsAsync(string customerId);
        Task<Booking?> GetBookingByIdAsync(string bookingId);
        Task<Booking> CreateBookingAsync(CreateBookingRequest request);
        Task<Booking> ReturnTrailerAsync(ReturnTrailerRequest request);
        Task ResetDataAsync();
    }

    public class BookingService : IBookingService
    {
        private readonly string _dataPath;
        private readonly ILogger<BookingService> _logger;
        private readonly ITrailerService _trailerService;
        private readonly INotificationService _notificationService;

        public BookingService(
            IWebHostEnvironment environment, 
            ILogger<BookingService> logger,
            ITrailerService trailerService,
            INotificationService notificationService)
        {
            _dataPath = Path.Combine(environment.ContentRootPath, "Data", "bookings.json");
            _logger = logger;
            _trailerService = trailerService;
            _notificationService = notificationService;
        }

        public async Task<List<Booking>> GetAllBookingsAsync()
        {
            try
            {
                if (!File.Exists(_dataPath))
                {
                    _logger.LogWarning("Bookings data file not found at {Path}", _dataPath);
                    return new List<Booking>();
                }

                var json = await File.ReadAllTextAsync(_dataPath);
                var bookings = JsonSerializer.Deserialize<List<Booking>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return bookings ?? new List<Booking>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading bookings data");
                return new List<Booking>();
            }
        }

        public async Task<List<Booking>> GetCustomerBookingsAsync(string customerId)
        {
            var bookings = await GetAllBookingsAsync();
            return bookings.Where(b => b.CustomerId == customerId).OrderByDescending(b => b.CreatedAt).ToList();
        }

        public async Task<Booking?> GetBookingByIdAsync(string bookingId)
        {
            var bookings = await GetAllBookingsAsync();
            return bookings.FirstOrDefault(b => b.Id == bookingId);
        }

        public async Task<Booking> CreateBookingAsync(CreateBookingRequest request)
        {
            var trailer = await _trailerService.GetTrailerByIdAsync(request.TrailerId);
            if (trailer == null)
            {
                throw new ArgumentException("Trailer not found");
            }

            if (trailer.Status != "Available")
            {
                throw new InvalidOperationException("Trailer is not available");
            }

            var booking = new Booking
            {
                Id = "BOOK" + DateTime.Now.Ticks.ToString()[^6..], // Generate simple ID
                CustomerId = request.CustomerId,
                CustomerName = request.CustomerName,
                CustomerEmail = request.CustomerEmail,
                TrailerId = request.TrailerId,
                LocationName = trailer.LocationName,
                TrailerNumber = trailer.TrailerNumber,
                BookingTime = request.BookingTime,
                ReturnTime = request.ReturnTime,
                Status = "Active",
                HasInsurance = request.HasInsurance,
                InsuranceFee = request.HasInsurance ? 50.0m : 0.0m,
                ExcessFee = 0.0m,
                TotalCost = request.HasInsurance ? 50.0m : 0.0m,
                CreatedAt = DateTime.UtcNow
            };

            var bookings = await GetAllBookingsAsync();
            bookings.Add(booking);
            await SaveBookingsAsync(bookings);

            // Update trailer status
            await _trailerService.UpdateTrailerStatusAsync(request.TrailerId, "Booked");

            // Send confirmation notification
            await _notificationService.CreateNotificationAsync(new Notification
            {
                Id = "NOTIF" + DateTime.Now.Ticks.ToString()[^6..],
                CustomerId = request.CustomerId,
                Type = "BookingConfirmation",
                Title = "Booking Confirmed",
                Message = $"Your trailer booking for {trailer.LocationName} - Trailer #{trailer.TrailerNumber} has been confirmed. Please return by {request.ReturnTime:HH:mm} to avoid excess fees.",
                Timestamp = DateTime.UtcNow,
                IsRead = false,
                Priority = "High"
            });

            _logger.LogInformation("Created booking {BookingId} for trailer {TrailerId}", booking.Id, request.TrailerId);
            return booking;
        }

        public async Task<Booking> ReturnTrailerAsync(ReturnTrailerRequest request)
        {
            var booking = await GetBookingByIdAsync(request.BookingId);
            if (booking == null)
            {
                throw new ArgumentException("Booking not found");
            }

            if (booking.Status != "Active")
            {
                throw new InvalidOperationException("Booking is not active");
            }

            booking.ActualReturnTime = request.ReturnTime;
            booking.Status = "Completed";

            // Calculate excess fee if returned late
            var excessFeeRate = 100.0m; // 100 DKK per hour late
            if (booking.IsLate(request.ReturnTime))
            {
                var lateDuration = booking.GetLatenessDuration(request.ReturnTime);
                var lateHours = Math.Ceiling(lateDuration.TotalHours);
                booking.ExcessFee = (decimal)lateHours * excessFeeRate;
                booking.TotalCost += booking.ExcessFee;

                // Send late return notification
                await _notificationService.CreateNotificationAsync(new Notification
                {
                    Id = "NOTIF" + DateTime.Now.Ticks.ToString()[^6..],
                    CustomerId = booking.CustomerId,
                    Type = "LateFee",
                    Title = "Late Return Fee Applied",
                    Message = $"Your trailer was returned {lateDuration.TotalHours:F1} hours late. An excess fee of {booking.ExcessFee:F2} DKK has been applied. Please contact Customer Service for payment.",
                    Timestamp = DateTime.UtcNow,
                    IsRead = false,
                    Priority = "High"
                });
            }
            else
            {
                // Send normal return confirmation
                await _notificationService.CreateNotificationAsync(new Notification
                {
                    Id = "NOTIF" + DateTime.Now.Ticks.ToString()[^6..],
                    CustomerId = booking.CustomerId,
                    Type = "ReturnConfirmation",
                    Title = "Trailer Returned Successfully",
                    Message = $"Thank you for returning your trailer on time! Total cost: {booking.TotalCost:F2} DKK.",
                    Timestamp = DateTime.UtcNow,
                    IsRead = false,
                    Priority = "Medium"
                });
            }

            var bookings = await GetAllBookingsAsync();
            var bookingIndex = bookings.FindIndex(b => b.Id == booking.Id);
            if (bookingIndex >= 0)
            {
                bookings[bookingIndex] = booking;
                await SaveBookingsAsync(bookings);
            }

            // Make trailer available again
            await _trailerService.UpdateTrailerStatusAsync(booking.TrailerId, "Available");

            _logger.LogInformation("Returned trailer for booking {BookingId}, excess fee: {ExcessFee}", booking.Id, booking.ExcessFee);
            return booking;
        }

        public async Task ResetDataAsync()
        {
            var defaultBookings = new List<Booking>
            {
                new Booking
                {
                    Id = "BOOK001",
                    CustomerId = "CUST001",
                    CustomerName = "John Doe",
                    CustomerEmail = "john.doe@email.com",
                    TrailerId = "LOC001-001",
                    LocationName = "Jem og Fix Nørrebro",
                    TrailerNumber = 1,
                    BookingTime = DateTime.Today.AddHours(14),
                    ReturnTime = DateTime.Today.AddHours(23).AddMinutes(59),
                    Status = "Active",
                    HasInsurance = true,
                    InsuranceFee = 50.0m,
                    ExcessFee = 0.0m,
                    TotalCost = 50.0m,
                    CreatedAt = DateTime.Today.AddHours(13).AddMinutes(45)
                }
            };

            await SaveBookingsAsync(defaultBookings);
            _logger.LogInformation("Reset bookings data to default");
        }

        private async Task SaveBookingsAsync(List<Booking> bookings)
        {
            var json = JsonSerializer.Serialize(bookings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(_dataPath, json);
        }
    }
}