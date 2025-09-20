namespace MyTrailerSystem.Models
{
    public class Trailer
    {
        public string Id { get; set; } = string.Empty;
        public string LocationId { get; set; } = string.Empty;
        public int TrailerNumber { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Available, Booked, Maintenance
        public DateTime LastMaintenance { get; set; }
        public GPSCoordinates GPS { get; set; } = new();
    }

    public class GPSCoordinates
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}