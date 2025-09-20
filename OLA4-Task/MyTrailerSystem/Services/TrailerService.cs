using MyTrailerSystem.Models;
using System.Text.Json;

namespace MyTrailerSystem.Services
{
    public interface ITrailerService
    {
        Task<List<Trailer>> GetAllTrailersAsync();
        Task<Trailer?> GetTrailerByIdAsync(string trailerId);
        Task<List<Trailer>> GetAvailableTrailersAsync();
        Task UpdateTrailerStatusAsync(string trailerId, string status);
        Task ResetDataAsync();
    }

    public class TrailerService : ITrailerService
    {
        private readonly string _dataPath;
        private readonly ILogger<TrailerService> _logger;

        public TrailerService(IWebHostEnvironment environment, ILogger<TrailerService> logger)
        {
            _dataPath = Path.Combine(environment.ContentRootPath, "Data", "trailers.json");
            _logger = logger;
        }

        public async Task<List<Trailer>> GetAllTrailersAsync()
        {
            try
            {
                if (!File.Exists(_dataPath))
                {
                    _logger.LogWarning("Trailers data file not found at {Path}", _dataPath);
                    return new List<Trailer>();
                }

                var json = await File.ReadAllTextAsync(_dataPath);
                var trailers = JsonSerializer.Deserialize<List<Trailer>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return trailers ?? new List<Trailer>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading trailers data");
                return new List<Trailer>();
            }
        }

        public async Task<Trailer?> GetTrailerByIdAsync(string trailerId)
        {
            var trailers = await GetAllTrailersAsync();
            return trailers.FirstOrDefault(t => t.Id == trailerId);
        }

        public async Task<List<Trailer>> GetAvailableTrailersAsync()
        {
            var trailers = await GetAllTrailersAsync();
            return trailers.Where(t => t.Status == "Available").ToList();
        }

        public async Task UpdateTrailerStatusAsync(string trailerId, string status)
        {
            try
            {
                var trailers = await GetAllTrailersAsync();
                var trailer = trailers.FirstOrDefault(t => t.Id == trailerId);
                
                if (trailer != null)
                {
                    trailer.Status = status;
                    await SaveTrailersAsync(trailers);
                    _logger.LogInformation("Updated trailer {TrailerId} status to {Status}", trailerId, status);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating trailer status for {TrailerId}", trailerId);
            }
        }

        public async Task ResetDataAsync()
        {
            var defaultTrailers = new List<Trailer>
            {
                new Trailer
                {
                    Id = "LOC001-001",
                    LocationId = "LOC001",
                    TrailerNumber = 1,
                    LocationName = "Jem og Fix Nørrebro",
                    Address = "Nørrebrogade 123, Copenhagen",
                    Status = "Available",
                    LastMaintenance = DateTime.UtcNow.AddDays(-14),
                    GPS = new GPSCoordinates { Latitude = 55.6868, Longitude = 12.5606 }
                },
                new Trailer
                {
                    Id = "LOC001-002",
                    LocationId = "LOC001",
                    TrailerNumber = 2,
                    LocationName = "Jem og Fix Nørrebro",
                    Address = "Nørrebrogade 123, Copenhagen",
                    Status = "Available",
                    LastMaintenance = DateTime.UtcNow.AddDays(-14),
                    GPS = new GPSCoordinates { Latitude = 55.6868, Longitude = 12.5606 }
                },
                new Trailer
                {
                    Id = "LOC002-001",
                    LocationId = "LOC002",
                    TrailerNumber = 1,
                    LocationName = "Fog Østerbro",
                    Address = "Østerbrogade 456, Copenhagen",
                    Status = "Available",
                    LastMaintenance = DateTime.UtcNow.AddDays(-17),
                    GPS = new GPSCoordinates { Latitude = 55.7008, Longitude = 12.5751 }
                }
            };

            await SaveTrailersAsync(defaultTrailers);
            _logger.LogInformation("Reset trailers data to default");
        }

        private async Task SaveTrailersAsync(List<Trailer> trailers)
        {
            var json = JsonSerializer.Serialize(trailers, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(_dataPath, json);
        }
    }
}