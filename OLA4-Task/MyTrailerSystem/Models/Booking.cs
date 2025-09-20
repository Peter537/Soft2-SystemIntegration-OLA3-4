namespace MyTrailerSystem.Models
{
    public class Booking
    {
        public string Id { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string TrailerId { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public int TrailerNumber { get; set; }
        public DateTime BookingTime { get; set; }
        public DateTime ReturnTime { get; set; }
        public DateTime? ActualReturnTime { get; set; }
        public string Status { get; set; } = string.Empty; // Active, Completed, Cancelled
        public bool HasInsurance { get; set; }
        public decimal InsuranceFee { get; set; }
        public decimal ExcessFee { get; set; }
        public decimal TotalCost { get; set; }
        public DateTime CreatedAt { get; set; }

        public bool IsLate(DateTime currentTime)
        {
            return ActualReturnTime.HasValue && ActualReturnTime.Value > ReturnTime;
        }

        public bool IsOverdue(DateTime currentTime)
        {
            return !ActualReturnTime.HasValue && currentTime > ReturnTime;
        }

        public TimeSpan GetLatenessDuration(DateTime currentTime)
        {
            if (ActualReturnTime.HasValue && ActualReturnTime.Value > ReturnTime)
            {
                return ActualReturnTime.Value - ReturnTime;
            }
            else if (!ActualReturnTime.HasValue && currentTime > ReturnTime)
            {
                return currentTime - ReturnTime;
            }
            return TimeSpan.Zero;
        }
    }
}