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
    private const string DefaultUrl =
        "https://data.gov.il/api/3/action/datastore_search?resource_id=e83f763b-b7d7-479e-b172-ae981ddc6de5&limit=500";

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
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                log.LogWarning(
                    ex,
                    "Israel Airports Authority feed unavailable; seeded data remains available");
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(
                        cfg.GetValue("FLIGHTS_INTERVAL_SECONDS", 900)),
                    ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task<int> CollectLive(CancellationToken ct)
    {
        var url = cfg["ISRAEL_FLIGHTS_API_URL"] ?? DefaultUrl;

        var json = await clients.CreateClient("iaa")
            .GetStringAsync(url, ct);

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
                    Name = string.IsNullOrWhiteSpace(Text(r, "CHOPERD"))
                        ? code
                        : Text(r, "CHOPERD"),
                    IataCode = code[..Math.Min(3, code.Length)],
                    Country = code is "LY" or "6H" or "IZ" or "2U"
                        ? "Israel"
                        : "International"
                };

                db.Airlines.Add(airline);
                airlines[code] = airline;
            }

            var scheduled = Date(r, "CHSTOL");
            var actual = Date(r, "CHPTOL");

            var arrival = Text(r, "CHAORD")
                .Equals("A", StringComparison.OrdinalIgnoreCase);

            var other = Text(r, "CHLOC1")
                .Trim()
                .ToUpperInvariant();

            var number = code + Text(r, "CHFLTN").Trim();

            if (string.IsNullOrWhiteSpace(number))
                continue;

            var scheduledTime = scheduled ?? actual ?? DateTime.UtcNow;
            var actualTime = actual ?? scheduled ?? scheduledTime;

            var id = StableGuid(
                $"iaa|{number}|{scheduledTime:O}|{arrival}");

            events.Add(new FlightEvent(
                id,
                airline.Id,
                number,
                arrival ? other : "TLV",
                arrival ? "TLV" : other,
                arrival ? scheduledTime : actualTime,
                arrival ? actualTime : scheduledTime,
                Text(r, "CHRMINE"),
                Number(r, "CHTERM"),
                arrival,
                "Israel Airports Authority via data.gov.il",
                DateTime.UtcNow));
        }

        await db.SaveChangesAsync(ct);

        foreach (var flightEvent in events)
        {
            await KafkaSupport.PublishAsync(
                cfg["KAFKA_BOOTSTRAP_SERVERS"] ?? "kafka:9092",
                "airline.flights",
                flightEvent,
                ct);
        }

        return events.Count;
    }

    private static string Text(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var value) &&
               value.ValueKind is not JsonValueKind.Null
                   and not JsonValueKind.Undefined
            ? value.ToString()
            : string.Empty;
    }

    private static DateTime? Date(JsonElement element, string name)
    {
        var value = Text(element, name);

        return DateTime.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal |
            DateTimeStyles.AdjustToUniversal,
            out var result)
                ? result
                : null;
    }

    private static int? Number(JsonElement element, string name)
    {
        var value = Text(element, name);

        return int.TryParse(
            value,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var result)
                ? result
                : null;
    }

    private static Guid StableGuid(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return new Guid(bytes.AsSpan(0, 16));
    }
}