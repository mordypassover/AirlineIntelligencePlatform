using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Shared;

namespace AnalyticsService;

public class Worker(
    ILogger<Worker> log,
    IConfiguration cfg,
    IServiceScopeFactory scopes,
    OverallScoreCalculator calc) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var c = KafkaSupport.Consumer(
                    cfg["KAFKA_BOOTSTRAP_SERVERS"] ?? "kafka:9092",
                    "analytics-service");

                c.Subscribe(
                [
                    "airline.sentiment",
                    "airline.prices",
                    "airline.flights",
                    "airline.environment"
                ]);

                while (!ct.IsCancellationRequested)
                {
                    var m = c.Consume(ct);

                    try
                    {
                        using var s = scopes.CreateScope();

                        var db = s.ServiceProvider
                            .GetRequiredService<AirlineDbContext>();

                        Guid aid = Guid.Empty;
                        Guid eid = Guid.Empty;

                        switch (m.Topic)
                        {
                            case "airline.sentiment":
                                var se = JsonSerializer.Deserialize<SentimentEvent>(
                                    m.Message.Value)!;

                                aid = se.AirlineId;
                                eid = se.EventId;

                                if (!await db.SentimentResults.AnyAsync(
                                    x => x.EventId == eid, ct))
                                {
                                    var review = await db.Reviews
                                        .SingleOrDefaultAsync(
                                            x => x.EventId == se.SourceEventId, ct);

                                    if (review is null)
                                    {
                                        review = new Review
                                        {
                                            Id = Guid.NewGuid(),
                                            EventId = se.SourceEventId,
                                            AirlineId = aid,
                                            Rating = se.Rating,
                                            ReviewText = "Collected review",
                                            CreatedAt = se.CreatedAt
                                        };

                                        db.Add(review);
                                    }

                                    db.Add(new SentimentResult
                                    {
                                        EventId = eid,
                                        ReviewId = review.Id,
                                        AirlineId = aid,
                                        Sentiment = se.Sentiment,
                                        Topics = string.Join(',', se.Topics),
                                        CreatedAt = se.CreatedAt
                                    });
                                }

                                break;

                            case "airline.prices":
                                var p = JsonSerializer.Deserialize<PriceEvent>(
                                    m.Message.Value)!;

                                aid = p.AirlineId;
                                eid = p.EventId;

                                if (!await db.FlightPrices.AnyAsync(
                                    x => x.EventId == eid, ct))
                                {
                                    db.Add(new FlightPrice
                                    {
                                        EventId = eid,
                                        AirlineId = aid,
                                        Origin = p.Origin,
                                        Destination = p.Destination,
                                        Price = p.Price,
                                        Currency = p.Currency,
                                        TravelDate = p.TravelDate,
                                        CollectedAt = p.CollectedAt
                                    });
                                }

                                break;

                            case "airline.flights":
                                var f = JsonSerializer.Deserialize<FlightEvent>(
                                    m.Message.Value)!;

                                aid = f.AirlineId;
                                eid = f.EventId;

                                if (!await db.Flights.AnyAsync(
                                    x => x.EventId == eid, ct))
                                {
                                    db.Add(new Flight
                                    {
                                        EventId = eid,
                                        AirlineId = aid,
                                        FlightNumber = f.FlightNumber,
                                        Origin = f.Origin,
                                        Destination = f.Destination,
                                        DepartureTime = f.DepartureTime,
                                        ArrivalTime = f.ArrivalTime,
                                        CollectedAt = f.CollectedAt
                                    });
                                }

                                break;

                            default:
                                var e = JsonSerializer.Deserialize<EnvironmentEvent>(
                                    m.Message.Value)!;

                                aid = e.AirlineId;
                                eid = e.EventId;

                                if (!await db.EnvironmentalMeasurements.AnyAsync(
                                    x => x.EventId == eid, ct))
                                {
                                    db.Add(new EnvironmentalMeasurement
                                    {
                                        EventId = eid,
                                        AirlineId = aid,
                                        Co2KgPerPassengerKm = e.Co2KgPerPassengerKm,
                                        FuelEfficiencyScore = e.FuelEfficiencyScore,
                                        MeasuredAt = e.MeasuredAt
                                    });
                                }

                                break;
                        }

                        db.Add(new RawData
                        {
                            EventId = eid,
                            Topic = m.Topic,
                            Payload = m.Message.Value,
                            Processed = true
                        });

                        await db.SaveChangesAsync(ct);

                        await Recalculate(db, aid, ct);
                    }
                    catch (JsonException ex)
                    {
                        log.LogWarning(
                            ex,
                            "Skipping malformed {Topic} event",
                            m.Topic);
                    }
                    catch (DbUpdateException ex)
                    {
                        log.LogWarning(ex, "Duplicate or invalid event skipped");
                    }
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Analytics consumer reconnecting");
                await Task.Delay(3000, ct);
            }
        }
    }

    async Task Recalculate(
        AirlineDbContext db,
        Guid id,
        CancellationToken ct)
    {
        var st = await db.AirlineStatistics.FindAsync([id], ct)
            ?? new AirlineStatistic { AirlineId = id };

        var reviews = await db.Reviews
            .Where(x => x.AirlineId == id)
            .ToListAsync(ct);

        var sentiments = await db.SentimentResults
            .Where(x => x.AirlineId == id)
            .ToListAsync(ct);

        var prices = await db.FlightPrices
            .Where(x => x.AirlineId == id)
            .ToListAsync(ct);

        var flights = await db.Flights
            .Where(x => x.AirlineId == id)
            .ToListAsync(ct);

        var env = await db.EnvironmentalMeasurements
            .Where(x => x.AirlineId == id)
            .ToListAsync(ct);

        if (reviews.Count > 0)
        {
            st.AverageRating = Math.Round(
                (decimal)reviews.Average(x => x.Rating), 2);
        }

        if (sentiments.Count > 0)
        {
            st.PositiveSentimentPercent = Math.Round(
                sentiments.Count(x => x.Sentiment == "Positive")
                * 100m / sentiments.Count, 1);
        }

        if (prices.Count > 0)
        {
            var normalized = prices
                .Select(x => x.Currency == "ILS" ? x.Price / 3.7m : x.Price)
                .ToList();

            st.AveragePrice = normalized.Average();
            st.LowestPrice = normalized.Min();
            st.HighestPrice = normalized.Max();
        }

        st.FlightsPerWeek = flights.Count(
            x => x.DepartureTime > DateTime.UtcNow.AddDays(-7));

        st.DestinationCount = flights
            .Select(x => x.Destination)
            .Distinct()
            .Count();

        st.RouteCount = flights
            .Select(x => x.Origin + "-" + x.Destination)
            .Distinct()
            .Count();

        if (env.Count > 0)
        {
            st.AverageCo2 = env.Average(x => x.Co2KgPerPassengerKm);
            st.EnvironmentalScore = env.Average(x => x.FuelEfficiencyScore);
        }

        st.PassengerExperienceScore = st.AverageRating * 20;
        st.PriceScore = Math.Clamp(100 - st.AveragePrice / 10, 0, 100);
        st.AvailabilityScore = Math.Clamp(st.FlightsPerWeek * 5, 0, 100);
        st.StaffScore = st.PassengerExperienceScore;
        st.ServiceScore = st.PassengerExperienceScore;

        st.OverallScore = calc.Calculate(
            new(
                st.PassengerExperienceScore,
                st.PriceScore,
                st.EnvironmentalScore,
                st.AvailabilityScore));

        st.UpdatedAt = DateTime.UtcNow;

        if (db.Entry(st).State == EntityState.Detached)
            db.Add(st);

        var today = DateTime.UtcNow.Date;

        var snapshot = await db.AirlineStatisticsHistory
            .Where(h => h.AirlineId == id && h.SnapshotAt >= today)
            .OrderByDescending(h => h.SnapshotAt)
            .FirstOrDefaultAsync(ct);

        if (snapshot is null)
        {
            snapshot = new AirlineStatisticHistory
            {
                AirlineId = id
            };

            db.Add(snapshot);
        }

        snapshot.AverageRating = st.AverageRating;
        snapshot.PositiveSentimentPercent = st.PositiveSentimentPercent;
        snapshot.AveragePrice = st.AveragePrice;
        snapshot.EnvironmentalScore = st.EnvironmentalScore;
        snapshot.FlightsPerWeek = st.FlightsPerWeek;
        snapshot.OverallScore = st.OverallScore;
        snapshot.SnapshotAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
    }
}