using Microsoft.EntityFrameworkCore;
using Shared;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FlightCollector;

public class Worker(
    ILogger<Worker> log,
    IServiceScopeFactory scopes,
    IHttpClientFactory clients,
    IConfiguration cfg) : BackgroundService
{
    const string DefaultUrl =
        "https://data.gov.il/api/3/action/datastore_search?resource_id=e83f763b-b7d7-479e-b172-ae981ddc6de5&limit=500";

    static readonly TimeZoneInfo IsraelTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Asia/Jerusalem");

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var count = await CollectLive(ct);

                log.LogInformation(
                    "Published {Count} live Israel flight events from data.gov.il",
                    count);
            }
            catch (Exception ex)
            {
                log.LogWarning(
                    ex,
                    "Israel Airports Authority feed unavailable; seeded data remains available");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(cfg.GetValue("FLIGHTS_INTERVAL_SECONDS", 900)),
                ct);
        }
    }

    async Task<int> CollectLive(CancellationToken ct)
    {
        var json = await clients.CreateClient("iaa")
            .GetStringAsync(cfg["ISRAEL_FLIGHTS_API_URL"] ?? DefaultUrl, ct);

        using var doc = JsonDocument.Parse(json);

        var records = doc.RootElement
            .GetProperty("result")
            .GetProperty("records");

        using var scope = scopes.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AirlineDbContext>();

        var airlines = await db.Airlines
            .ToDictionaryAsync(
                a => a.IataCode,
                StringComparer.OrdinalIgnoreCase,
                ct);

        var destinations = await db.Destinations
            .ToDictionaryAsync(
                d => d.Code,
                StringComparer.OrdinalIgnoreCase,
                ct);

        var events = new List<FlightEvent>();

        foreach (var r in records.EnumerateArray())
        {
            var code = Text(r, "CHOPER")
                .Trim()
                .ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(code))
                continue;

            if (!airlines.TryGetValue(code, out var airline))
            {
                airline = new Airline
                {
                    Name = Text(r, "CHOPERD") is { Length: > 0 } n
                        ? n
                        : code,
                    IataCode = code[..Math.Min(3, code.Length)],
                    Country = code is "LY" or "6H" or "IZ" or "2U"
                        ? "Israel"
                        : "International"
                };

                db.Airlines.Add(airline);
                db.AirlineStatistics.Add(new()
                {
                    AirlineId = airline.Id
                });

                airlines[code] = airline;
            }

            var scheduled = Date(r, "CHSTOL");
            var actual = Date(r, "CHPTOL");
            var arrival = Text(r, "CHAORD")
                .Equals("A", StringComparison.OrdinalIgnoreCase);

            var other = Text(r, "CHLOC1").ToUpperInvariant();
            var destinationName = Text(r, "CHLOC1D").Trim();
            var destinationCountry = Text(r, "CHLOCCT").Trim();

            if (other.Length == 3 && !destinations.ContainsKey(other))
            {
                var destination = new Destination
                {
                    Code = other,
                    City = string.IsNullOrWhiteSpace(destinationName)
                        ? other
                        : destinationName,
                    Country = destinationCountry
                };

                db.Destinations.Add(destination);
                destinations[other] = destination;
            }

            var number = code + Text(r, "CHFLTN").Trim();

            var id = Stable($"iaa|{number}|{scheduled:o}|{arrival}");

            events.Add(new FlightEvent(
                id,
                airline.Id,
                number,
                arrival ? other : "TLV",
                arrival ? "TLV" : other,
                arrival ? scheduled : actual,
                arrival ? actual : scheduled,
                Text(r, "CHRMINE"),
                Number(r, "CHTERM"),
                arrival,
                "Israel Airports Authority via data.gov.il",
                DateTime.UtcNow));
        }

        var eventIds = events.Select(e => e.EventId).ToList();
        var existingEventIds = (await db.Flights
            .Where(f => eventIds.Contains(f.EventId))
            .Select(f => f.EventId)
            .ToListAsync(ct))
            .ToHashSet();

        db.Flights.AddRange(events
            .Where(e => !existingEventIds.Contains(e.EventId))
            .Select(e => new Flight
            {
                EventId = e.EventId,
                AirlineId = e.AirlineId,
                FlightNumber = e.FlightNumber,
                Origin = e.Origin,
                Destination = e.Destination,
                DepartureTime = e.DepartureTime,
                ArrivalTime = e.ArrivalTime,
                Status = e.Status,
                Terminal = e.Terminal,
                IsArrival = e.IsArrival,
                Source = e.Source,
                CollectedAt = e.CollectedAt
            }));

        await db.SaveChangesAsync(ct);

        foreach (var e in events)
        {
            await KafkaSupport.PublishAsync(
                cfg["KAFKA_BOOTSTRAP_SERVERS"] ?? "kafka:9092",
                "airline.flights",
                e,
                ct);
        }

        return events.Count;
    }

    static string Text(JsonElement e, string n) =>
        e.TryGetProperty(n, out var v) && v.ValueKind != JsonValueKind.Null
            ? v.ToString()
            : "";

    static DateTime Date(JsonElement e, string n)
    {
        if (!DateTime.TryParse(
                Text(e, n),
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out var value))
        {
            return DateTime.UtcNow;
        }

        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => TimeZoneInfo.ConvertTimeToUtc(value, IsraelTimeZone)
        };
    }

    static int? Number(JsonElement e, string n) =>
        int.TryParse(Text(e, n), out var value)
            ? value
            : null;

    static Guid Stable(string value) =>
        new(MD5.HashData(Encoding.UTF8.GetBytes(value)));
}
