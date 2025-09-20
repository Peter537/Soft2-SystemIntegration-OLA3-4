namespace MyTrailerSystem.Models
{
    public class CreateBookingRequest
    {
        public string CustomerId { get; set; } = "CUST001"; // Default customer for demo
        public string CustomerName { get; set; } = "John Doe";
        public string CustomerEmail { get; set; } = "john.doe@email.com";
        public string TrailerId { get; set; } = string.Empty;
        public DateTime BookingTime { get; set; } = DateTime.Now;
        public DateTime ReturnTime { get; set; } = DateTime.Today.AddHours(23).AddMinutes(59); // Default to midnight
        public bool HasInsurance { get; set; } = false;
    }

    public class ReturnTrailerRequest
    {
        public string BookingId { get; set; } = string.Empty;
        public DateTime ReturnTime { get; set; } = DateTime.Now;
    }
}