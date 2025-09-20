namespace MyTrailerSystem.Models
{
    public class Notification
    {
        public string Id { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // BookingConfirmation, BookingReminder, ReturnConfirmation, LateFee, etc.
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; }
        public string Priority { get; set; } = string.Empty; // High, Medium, Low
    }
}