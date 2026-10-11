using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Shared;
using System.Text.Json;

[ApiController]
[Route("api/airlines")]
public class AirlinesController(
    AirlineDbContext db,
    IDistributedCache cache,
    IConfiguration cfg) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(string? search, string? country, decimal? minScore)
    {
        var q = db.Airlines
            .Where(a => a.IsActive)
            .Include(a => a.Statistics)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(a =>
                a.Name.ToLower().Contains(search.ToLower()) ||
                a.IataCode.ToLower().Contains(search.ToLower()));

        if (!string.IsNullOrWhiteSpace(country))
            q = q.Where(a => a.Country == country);

        if (minScore.HasValue)
            q = q.Where(a => a.Statistics!.OverallScore >= minScore);

        var rows = await q
            .OrderByDescending(a => a.Statistics!.OverallScore)
            .ToListAsync();

        return Ok(rows.Select(ApiView.Airline));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> One(Guid id)
    {
        var a = await db.Airlines
            .Include(a => a.Statistics)
            .SingleOrDefaultAsync(a => a.Id == id && a.IsActive);

        return a is null
            ? NotFound(new { message = "Airline not found" })
            : Ok(ApiView.Airline(a));
    }

    [HttpGet("{id:guid}/statistics")]
    public async Task<IActionResult> Statistics(Guid id)
    {
        var key = $"statistics:{id}";

        try
        {
            var cached = await cache.GetStringAsync(key);

            if (cached is not null)
                return Content(cached, "application/json");
        }
        catch
        {
        }

        var s = await db.AirlineStatistics
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.AirlineId == id);

        if (s is null)
            return NotFound(new { message = "Statistics not found" });

        var points = await db.AirlineStatisticsHistory
            .Where(h => h.AirlineId == id)
            .OrderBy(h => h.SnapshotAt)
            .Select(h => new
            {
                h.SnapshotAt,
                h.OverallScore,
                h.AverageRating,
                h.PositiveSentimentPercent,
                h.AveragePrice,
                h.EnvironmentalScore,
                h.FlightsPerWeek
            })
            .ToListAsync();

        var history = points
            .GroupBy(h => new { h.SnapshotAt.Year, h.SnapshotAt.Month })
            .Select(g => g.OrderByDescending(h => h.SnapshotAt).First())
            .OrderBy(h => h.SnapshotAt)
            .TakeLast(6)
            .ToList();

        if (history.Count == 0)
        {
            history.Add(new
            {
                SnapshotAt = s.UpdatedAt,
                s.OverallScore,
                s.AverageRating,
                s.PositiveSentimentPercent,
                s.AveragePrice,
                s.EnvironmentalScore,
                s.FlightsPerWeek
            });
        }

        var json = JsonSerializer.Serialize(
            new { current = s, history },
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

        try
        {
            await cache.SetStringAsync(
                key,
                json,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(
                        cfg.GetValue("STATISTICS_CACHE_SECONDS", 60))
                });
        }
        catch
        {
        }

        return Content(json, "application/json");
    }

    [HttpGet("{id:guid}/reviews")]
    public Task<List<object>> Reviews(Guid id) =>
        db.Reviews
            .Where(r => r.AirlineId == id)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => (object)new
            {
                r.Id,
                r.Rating,
                r.ReviewText,
                r.CreatedAt,
                sentiment = r.SentimentResult!.Sentiment,
                topics = r.SentimentResult.Topics
            })
            .ToListAsync();

    [HttpGet("{id:guid}/prices")]
    public async Task<IActionResult> Prices(Guid id) =>
        Ok(await db.FlightPrices
            .Where(x => x.AirlineId == id)
            .OrderByDescending(x => x.CollectedAt)
            .Take(100)
            .Select(x => new
            {
                x.Id,
                x.Origin,
                x.Destination,
                x.Price,
                x.Currency,
                x.TravelDate,
                x.CollectedAt
            })
            .ToListAsync());

    [HttpGet("{id:guid}/flights")]
    public async Task<IActionResult> Flights(Guid id) =>
        Ok(await db.Flights
            .Where(x => x.AirlineId == id)
            .OrderByDescending(x => x.CollectedAt)
            .Take(100)
            .Select(x => new
            {
                x.Id,
                x.FlightNumber,
                x.Origin,
                x.Destination,
                x.DepartureTime,
                x.ArrivalTime,
                x.Status,
                x.Terminal,
                x.IsArrival,
                x.Source,
                x.CollectedAt
            })
            .ToListAsync());

    [HttpGet("{id:guid}/environment")]
    public async Task<IActionResult> Environment(Guid id) =>
        Ok(await db.EnvironmentalMeasurements
            .Where(x => x.AirlineId == id)
            .OrderByDescending(x => x.MeasuredAt)
            .Take(100)
            .Select(x => new
            {
                x.Id,
                x.Co2KgPerPassengerKm,
                x.FuelEfficiencyScore,
                x.MeasuredAt
            })
            .ToListAsync());
}