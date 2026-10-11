using System.Text.Json;
using Shared;

namespace SentimentService;

public class Worker(
    ILogger<Worker> log,
    IConfiguration cfg,
    SentimentAnalyzer analyzer) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var c = KafkaSupport.Consumer(
                    cfg["KAFKA_BOOTSTRAP_SERVERS"] ?? "kafka:9092",
                    "sentiment-service");

                c.Subscribe("airline.reviews");

                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        var x = JsonSerializer.Deserialize<ReviewEvent>(
                            c.Consume(ct).Message.Value);

                        if (x is null)
                            continue;

                        var a = analyzer.Analyze(x.ReviewText);

                        await KafkaSupport.PublishAsync(
                            cfg["KAFKA_BOOTSTRAP_SERVERS"] ?? "kafka:9092",
                            "airline.sentiment",
                            new SentimentEvent(
                                Guid.NewGuid(),
                                x.EventId,
                                x.AirlineId,
                                x.Rating,
                                a.Sentiment,
                                a.Topics,
                                x.CreatedAt),
                            ct);
                    }
                    catch (JsonException ex)
                    {
                        log.LogWarning(ex, "Skipping malformed review event");
                    }
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Kafka unavailable; reconnecting");
                await Task.Delay(3000, ct);
            }
        }
    }
}