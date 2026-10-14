using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared;

[ApiController]
[Route("api/destinations")]
public class DestinationsController(AirlineDbContext db) : ControllerBase
{
    [HttpGet]
    public Task<List<Destination>> List() =>
        db.Destinations
            .OrderBy(d => d.City)
            .ToListAsync();

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> One(Guid id) =>
        await db.Destinations.FindAsync(id) is { } d
            ? Ok(d)
            : NotFound(new { message = "Destination not found" });

    [HttpGet("{id:guid}/airlines")]
    public async Task<IActionResult> Airlines(Guid id, string? origin, DateTime? date)
    {
        var d = await db.Destinations.FindAsync(id);

        if (d is null)
            return NotFound();

        var selectedOrigin = string.IsNullOrWhiteSpace(origin)
            ? "TLV"
            : origin.ToUpperInvariant();

        var q = db.Flights
            .Where(f =>
                (f.Origin == selectedOrigin && f.Destination == d.Code) ||
                (f.Origin == d.Code && f.Destination == selectedOrigin));

        var ids = q.Select(f => f.AirlineId).Distinct();

        var airlines = await db.Airlines
            .Include(a => a.Statistics)
            .Where(a => ids.Contains(a.Id))
            .ToListAsync();

        var rows = new List<object>();

        var dayStart = date.HasValue
            ? DateTime.SpecifyKind(date.Value.Date, DateTimeKind.Utc)
            : (DateTime?)null;

        var dayEnd = dayStart?.AddDays(1);

        foreach (var a in airlines)
        {
            var fares = db.FlightPrices
                .Where(p =>
                    p.AirlineId == a.Id &&
                    p.Origin == selectedOrigin &&
                    p.Destination == d.Code);

            if (dayStart.HasValue)
            {
                fares = fares.Where(p =>
                    p.TravelDate >= dayStart.Value &&
                    p.TravelDate < dayEnd!.Value);
            }

            var fare = await fares
                .OrderBy(p => p.Price)
                .Select(p => new
                {
                    p.Price,
                    p.Currency,
                    p.TravelDate
                })
                .FirstOrDefaultAsync();

            var price = fare?.Price ?? EstimateFare(d, a, dayStart);

            rows.Add(new
            {
                airline = ApiView.Airline(a),
                price,
                currency = fare?.Currency ?? "USD",
                priceDate = fare?.TravelDate ?? dayStart,
                isEstimated = fare is null,
                flights = await db.Flights.CountAsync(f =>
                    f.AirlineId == a.Id &&
                    (
                        (f.Origin == selectedOrigin && f.Destination == d.Code) ||
                        (f.Origin == d.Code && f.Destination == selectedOrigin)
                    )),
                co2 = await db.EnvironmentalMeasurements
                    .Where(e => e.AirlineId == a.Id)
                    .OrderByDescending(e => e.MeasuredAt)
                    .Select(e => (decimal?)e.Co2KgPerPassengerKm)
                    .FirstOrDefaultAsync()
            });
        }

        return Ok(rows);
    }

    private static decimal EstimateFare(
        Destination destination,
        Airline airline,
        DateTime? travelDate)
    {
        var country = destination.Country.Trim().ToUpperInvariant();

        var baseFare = country switch
        {
            "ISRAEL" => 65m,
            "CYPRUS" or "GREECE" => 120m,
            "AZERBAIJAN" or "GEORGIA" or "UNITED ARAB EMIRATES" => 220m,
            "ETHIOPIA" or "RUSSIAN FEDERATION" => 320m,
            "JAPAN" or "THAILAND" or "UNITED STATES" => 720m,
            _ => 240m
        };

        var airlineAdjustment = airline.IataCode
            .ToUpperInvariant()
            .Sum(c => c) % 5 * 10m;

        var month = travelDate?.Month;
        var seasonalMultiplier = month switch
        {
            7 or 8 => 1.2m,
            12 => 1.15m,
            _ => 1m
        };

        return Math.Round(
            (baseFare + airlineAdjustment) * seasonalMultiplier / 5m,
            MidpointRounding.AwayFromZero) * 5m;
    }
}
