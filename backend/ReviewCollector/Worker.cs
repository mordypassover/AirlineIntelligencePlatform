using Microsoft.EntityFrameworkCore;
using Shared;

namespace ReviewCollector;

public class Worker(
    ILogger<Worker> log,
    IServiceScopeFactory scopes,
    IConfiguration cfg) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!cfg.GetValue("ENABLE_SYNTHETIC_REVIEW_STREAM", false))
        {
            log.LogInformation(
                "Static review seed mode is active; repeated synthetic reviews are disabled");
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return;
        }

        var texts = new[]
        {
            "Friendly staff and a comfortable seat. Excellent service.",
            "A delay and poor baggage handling, but helpful staff.",
            "Smooth flight, clean cabin and great service."
        };

        var n = 0;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var s = scopes.CreateScope();

                var ids = await s.ServiceProvider
                    .GetRequiredService<AirlineDbContext>()
                    .Airlines
                    .Select(a => a.Id)
                    .ToListAsync(ct);

                foreach (var id in ids)
                {
                    var e = new ReviewEvent(
                        Guid.NewGuid(),
                        id,
                        n % 3 == 1 ? 3 : 5,
                        texts[n++ % texts.Length],
                        DateTime.UtcNow);

                    await KafkaSupport.PublishAsync(
                        cfg["KAFKA_BOOTSTRAP_SERVERS"] ?? "kafka:9092",
                        "airline.reviews",
                        e,
                        ct);
                }

                log.LogInformation(
                    "Published {Count} deterministic review events",
                    ids.Count);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Review collection failed; retrying");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(cfg.GetValue("REVIEWS_INTERVAL_SECONDS", 30)),
                ct);
        }
    }
}
