using MyTrailerSystem.Models;
using System.Text.Json;

namespace MyTrailerSystem.Services
{
    public interface INotificationService
    {
        Task<List<Notification>> GetAllNotificationsAsync();
        Task<List<Notification>> GetCustomerNotificationsAsync(string customerId);
        Task<Notification> CreateNotificationAsync(Notification notification);
        Task MarkAsReadAsync(string notificationId);
        Task ResetDataAsync();
    }

    public class NotificationService : INotificationService
    {
        private readonly string _dataPath;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(IWebHostEnvironment environment, ILogger<NotificationService> logger)
        {
            _dataPath = Path.Combine(environment.ContentRootPath, "Data", "notifications.json");
            _logger = logger;
        }

        public async Task<List<Notification>> GetAllNotificationsAsync()
        {
            try
            {
                if (!File.Exists(_dataPath))
                {
                    _logger.LogWarning("Notifications data file not found at {Path}", _dataPath);
                    return new List<Notification>();
                }

                var json = await File.ReadAllTextAsync(_dataPath);
                var notifications = JsonSerializer.Deserialize<List<Notification>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return notifications ?? new List<Notification>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading notifications data");
                return new List<Notification>();
            }
        }

        public async Task<List<Notification>> GetCustomerNotificationsAsync(string customerId)
        {
            var notifications = await GetAllNotificationsAsync();
            return notifications.Where(n => n.CustomerId == customerId)
                              .OrderByDescending(n => n.Timestamp)
                              .ToList();
        }

        public async Task<Notification> CreateNotificationAsync(Notification notification)
        {
            var notifications = await GetAllNotificationsAsync();
            notifications.Add(notification);
            await SaveNotificationsAsync(notifications);

            _logger.LogInformation("Created notification {NotificationId} for customer {CustomerId}", 
                notification.Id, notification.CustomerId);
            
            return notification;
        }

        public async Task MarkAsReadAsync(string notificationId)
        {
            var notifications = await GetAllNotificationsAsync();
            var notification = notifications.FirstOrDefault(n => n.Id == notificationId);
            
            if (notification != null)
            {
                notification.IsRead = true;
                await SaveNotificationsAsync(notifications);
                _logger.LogInformation("Marked notification {NotificationId} as read", notificationId);
            }
        }

        public async Task ResetDataAsync()
        {
            var defaultNotifications = new List<Notification>
            {
                new Notification
                {
                    Id = "NOTIF001",
                    CustomerId = "CUST001",
                    Type = "BookingConfirmation",
                    Title = "Booking Confirmed",
                    Message = "Your trailer booking for Jem og Fix Nørrebro - Trailer #1 has been confirmed. Please return by 23:59 to avoid excess fees.",
                    Timestamp = DateTime.Today.AddHours(13).AddMinutes(45),
                    IsRead = false,
                    Priority = "High"
                },
                new Notification
                {
                    Id = "NOTIF002",
                    CustomerId = "CUST001",
                    Type = "BookingReminder",
                    Title = "Return Reminder",
                    Message = "Remember to return your trailer by 23:59 tonight to avoid excess fees.",
                    Timestamp = DateTime.Today.AddHours(20),
                    IsRead = false,
                    Priority = "Medium"
                }
            };

            await SaveNotificationsAsync(defaultNotifications);
            _logger.LogInformation("Reset notifications data to default");
        }

        private async Task SaveNotificationsAsync(List<Notification> notifications)
        {
            var json = JsonSerializer.Serialize(notifications, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(_dataPath, json);
        }
    }
}