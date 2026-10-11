using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Shared;

public static class StaticReviewSeeder
{
    private static readonly string[] LegacySyntheticReviewTexts =
    [
        "Friendly staff and a comfortable seat. Excellent service.",
        "A delay and poor baggage handling, but helpful staff.",
        "Smooth flight, clean cabin and great service.",
        "A delay and slow baggage handling, but helpful staff.",
        "Friendly staff, comfortable seat and excellent service."
    ];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<int> EnsureAsync(
        AirlineDbContext db,
        CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "SeedData",
            "airline-reviews.json");

        if (!File.Exists(path))
            throw new FileNotFoundException("Static airline review data was not found.", path);

        await using var stream = File.OpenRead(path);
        var catalogue = await JsonSerializer.DeserializeAsync<ReviewCatalogue>(
            stream,
            JsonOptions,
            cancellationToken) ?? throw new InvalidDataException("Static airline review data is empty.");

        var definitions = catalogue.Airlines.ToDictionary(
            x => x.IataCode,
            StringComparer.OrdinalIgnoreCase);

        var airlines = await db.Airlines
            .Include(x => x.Statistics)
            .ToListAsync(cancellationToken);

        var added = 0;
        var analyzer = new SentimentAnalyzer();

        var legacyReviews = await db.Reviews
            .Include(x => x.SentimentResult)
            .Where(x => LegacySyntheticReviewTexts.Contains(x.ReviewText))
            .ToListAsync(cancellationToken);

        if (legacyReviews.Count > 0)
        {
            db.SentimentResults.RemoveRange(
                legacyReviews
                    .Where(x => x.SentimentResult is not null)
                    .Select(x => x.SentimentResult!));
            db.Reviews.RemoveRange(legacyReviews);
            await db.SaveChangesAsync(cancellationToken);
        }

        foreach (var airline in airlines)
        {
            var reviewDefinitions = definitions.TryGetValue(airline.IataCode, out var definition)
                ? definition.Reviews
                : catalogue.DefaultReviews
                    .Select(x => x with
                    {
                        Text = x.Text.Replace(
                            "{airline}",
                            airline.Name,
                            StringComparison.OrdinalIgnoreCase)
                    })
                    .ToList();

            var existingTexts = (await db.Reviews
                .Where(x => x.AirlineId == airline.Id)
                .Select(x => x.ReviewText)
                .ToListAsync(cancellationToken))
                .ToHashSet(StringComparer.Ordinal);

            foreach (var definitionReview in reviewDefinitions)
            {
                if (!existingTexts.Add(definitionReview.Text))
                    continue;

                var review = new Review
                {
                    Id = Guid.NewGuid(),
                    EventId = Guid.NewGuid(),
                    AirlineId = airline.Id,
                    Rating = Math.Clamp(definitionReview.Rating, 1, 5),
                    ReviewText = definitionReview.Text,
                    CreatedAt = DateTime.UtcNow.Date.AddDays(-definitionReview.DaysAgo)
                };

                var analysis = analyzer.Analyze(review.ReviewText);

                db.Reviews.Add(review);
                db.SentimentResults.Add(new SentimentResult
                {
                    EventId = Guid.NewGuid(),
                    ReviewId = review.Id,
                    AirlineId = airline.Id,
                    Sentiment = analysis.Sentiment,
                    Topics = string.Join(',', analysis.Topics),
                    CreatedAt = review.CreatedAt
                });

                added++;
            }
        }

        if (added > 0)
            await db.SaveChangesAsync(cancellationToken);

        return added;
    }
}

public sealed class StaticReviewSeedWorker(
    IServiceScopeFactory scopes,
    ILogger<StaticReviewSeedWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AirlineDbContext>();
                var added = await StaticReviewSeeder.EnsureAsync(db, stoppingToken);

                if (added > 0)
                    logger.LogInformation("Added {Count} missing static airline reviews", added);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Static airline review seeding failed; retrying");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}

public sealed record ReviewCatalogue(
    int ReviewsPerAirline,
    List<ReviewDefinition> DefaultReviews,
    List<AirlineReviewDefinition> Airlines);

public sealed record AirlineReviewDefinition(
    string IataCode,
    string Name,
    List<ReviewDefinition> Reviews);

public sealed record ReviewDefinition(int Rating, int DaysAgo, string Text);
