using Microsoft.EntityFrameworkCore;
using Shared;

namespace PriceCollector;

public class Worker(
    ILogger<Worker> log,
    IServiceScopeFactory scopes,
    IConfiguration cfg) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var tick = 0;

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

                foreach (var (id, i) in ids.Select((x, i) => (x, i)))
                {
                    await KafkaSupport.PublishAsync(
                        cfg["KAFKA_BOOTSTRAP_SERVERS"] ?? "kafka:9092",
                        "airline.prices",
                        new PriceEvent(
                            Guid.NewGuid(),
                            id,
                            "LHR",
                            "JFK",
                            320 + i * 45 + (tick++ % 5) * 7,
                            "USD",
                            DateTime.UtcNow.Date.AddDays(30),
                            DateTime.UtcNow),
                        ct);
                }

                log.LogInformation("Published price events");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Price collection failed; retrying");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(cfg.GetValue("PRICES_INTERVAL_SECONDS", 20)),
                ct);
        }
    }
}