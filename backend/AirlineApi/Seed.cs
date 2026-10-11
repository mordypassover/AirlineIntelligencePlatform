using Microsoft.EntityFrameworkCore;
using Shared;

public static class Seed
{
    public static async Task Run(AirlineDbContext db, IConfiguration cfg)
    {
        if (await db.Airlines.AnyAsync())
        {
            await EnsureIsraelOrigins(db);
            return;
        }

        var defs = new[]
        {
            ("El Al Israel Airlines", "LY", "Israel", 86m, 4.2m, 78m, 84m, 76m, 420m, 128m),
            ("Israir Airlines", "6H", "Israel", 81m, 4.0m, 75m, 80m, 79m, 260m, 116m),
            ("Arkia Israeli Airlines", "IZ", "Israel", 80m, 3.9m, 73m, 79m, 81m, 245m, 112m),
            ("Sundor", "2U", "Israel", 78m, 3.8m, 71m, 77m, 78m, 230m, 119m),
            ("Air Haifa", "E2", "Israel", 80m, 4.0m, 76m, 80m, 84m, 190m, 94m),
            ("Aegean Airlines", "A3", "Greece", 85m, 4.3m, 82m, 85m, 83m, 215m, 105m),
            ("Wizz Air", "W6", "Hungary", 74m, 3.5m, 66m, 69m, 82m, 145m, 99m),
            ("Lufthansa", "LH", "Germany", 87m, 4.4m, 84m, 87m, 80m, 390m, 121m)
        };

        var airlines = new List<Airline>();
        var calc = new OverallScoreCalculator();

        foreach (var d in defs)
        {
            var a = new Airline
            {
                Name = d.Item1,
                IataCode = d.Item2,
                Country = d.Item3
            };

            airlines.Add(a);
            db.Add(a);

            var priceScore = Math.Clamp(100m - d.Item8 / 10m, 30, 95);
            var availability = Math.Clamp(d.Item6, 0, 100);

            var s = new AirlineStatistic
            {
                AirlineId = a.Id,
                AverageRating = d.Item5,
                PositiveSentimentPercent = d.Item6,
                StaffScore = d.Item7,
                ServiceScore = d.Item7 - 2,
                PositiveTopics = "Friendly staff, Comfortable aircraft, Good service",
                Complaints = "Delays, Customer support, Baggage handling",
                AveragePrice = d.Item8,
                LowestPrice = d.Item8 * .62m,
                HighestPrice = d.Item8 * 1.62m,
                FlightsPerWeek = (int)(d.Item6 * 1.6m),
                DestinationCount = (int)d.Item6,
                RouteCount = (int)(d.Item6 * 1.8m),
                EnvironmentalScore = d.Item7,
                AverageCo2 = d.Item9,
                PassengerExperienceScore = d.Item4,
                PriceScore = priceScore,
                AvailabilityScore = availability,
                OverallScore = calc.Calculate(
                    new(d.Item4, priceScore, d.Item7, availability))
            };

            db.Add(s);

            for (var h = 5; h >= 0; h--)
            {
                db.Add(new AirlineStatisticHistory
                {
                    AirlineId = a.Id,
                    AverageRating = s.AverageRating - h * .04m,
                    PositiveSentimentPercent = s.PositiveSentimentPercent - h * .7m,
                    AveragePrice = s.AveragePrice * (1 - h * .018m),
                    EnvironmentalScore = s.EnvironmentalScore - h * .3m,
                    FlightsPerWeek = s.FlightsPerWeek - h,
                    OverallScore = s.OverallScore - h * .35m,
                    SnapshotAt = DateTime.UtcNow.AddMonths(-h)
                });
            }

            for (var r = 0; r < 3; r++)
            {
                var review = new Review
                {
                    Id = Guid.NewGuid(),
                    EventId = Guid.NewGuid(),
                    AirlineId = a.Id,
                    Rating = r == 2 ? 3 : 5,
                    ReviewText = r == 2
                        ? "A delay and slow baggage handling, but helpful staff."
                        : "Friendly staff, comfortable seat and excellent service.",
                    CreatedAt = DateTime.UtcNow.AddDays(-r)
                };

                db.Add(review);

                db.Add(new SentimentResult
                {
                    EventId = Guid.NewGuid(),
                    ReviewId = review.Id,
                    AirlineId = a.Id,
                    Sentiment = r == 2 ? "Neutral" : "Positive",
                    Topics = r == 2
                        ? "Delays,Baggage,Staff"
                        : "Staff,Comfort,Service",
                    CreatedAt = review.CreatedAt
                });
            }
        }

        var dests = new[]
        {
            ("TLV", "Tel Aviv", "Israel"),
            ("ETM", "Eilat / Ramon", "Israel"),
            ("HFA", "Haifa", "Israel"),
            ("ATH", "Athens", "Greece"),
            ("LCA", "Larnaca", "Cyprus"),
            ("FCO", "Rome", "Italy"),
            ("FRA", "Frankfurt", "Germany"),
            ("LHR", "London", "United Kingdom")
        };

        foreach (var d in dests)
        {
            db.Add(new Destination
            {
                Code = d.Item1,
                City = d.Item2,
                Country = d.Item3
            });
        }

        foreach (var (a, i) in airlines.Select((a, i) => (a, i)))
        {
            for (var j = 0; j < dests.Length; j++)
            {
                if (dests[j].Item1 is "TLV" or "ETM" or "HFA")
                    continue;

                db.Add(new Flight
                {
                    EventId = Guid.NewGuid(),
                    AirlineId = a.Id,
                    FlightNumber = $"{a.IataCode}{120 + j}",
                    Origin = "TLV",
                    Destination = dests[j].Item1,
                    DepartureTime = DateTime.UtcNow.Date
                        .AddDays(1)
                        .AddHours(6 + j * 2),
                    ArrivalTime = DateTime.UtcNow.Date
                        .AddDays(1)
                        .AddHours(9 + j * 2),
                    Status = "Scheduled",
                    Terminal = 3,
                    IsArrival = false,
                    Source = "Seed demo",
                    CollectedAt = DateTime.UtcNow
                });

                db.Add(new FlightPrice
                {
                    EventId = Guid.NewGuid(),
                    AirlineId = a.Id,
                    Origin = "TLV",
                    Destination = dests[j].Item1,
                    Price = 90 + i * 35 + j * 18,
                    Currency = "USD",
                    TravelDate = DateTime.UtcNow.Date.AddDays(30),
                    CollectedAt = DateTime.UtcNow
                });
            }

            db.Add(new EnvironmentalMeasurement
            {
                EventId = Guid.NewGuid(),
                AirlineId = a.Id,
                Co2KgPerPassengerKm = defs[i].Item10,
                FuelEfficiencyScore = defs[i].Item7,
                MeasuredAt = DateTime.UtcNow
            });
        }

        var email = (cfg["ADMIN_EMAIL"] ?? "admin@airline.local").ToLower();

        db.Add(new User
        {
            Email = email,
            DisplayName = "Platform Admin",
            Role = "ADMIN",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(
                cfg["ADMIN_PASSWORD"] ?? "ChangeMe123!")
        });

        await db.SaveChangesAsync();
        await EnsureIsraelOrigins(db);
    }

    private static async Task EnsureIsraelOrigins(AirlineDbContext db)
    {
        var airports = new[]
        {
            ("TLV", "Tel Aviv"),
            ("ETM", "Eilat / Ramon"),
            ("HFA", "Haifa")
        };

        foreach (var airport in airports)
        {
            var existing = await db.Destinations
                .SingleOrDefaultAsync(d => d.Code == airport.Item1);

            if (existing is null)
            {
                db.Destinations.Add(new Destination
                {
                    Code = airport.Item1,
                    City = airport.Item2,
                    Country = "Israel"
                });
            }
            else
            {
                existing.City = airport.Item2;
                existing.Country = "Israel";
            }
        }

        await db.SaveChangesAsync();

        var airHaifa = await db.Airlines
            .SingleOrDefaultAsync(a => a.IataCode == "E2");

        if (airHaifa is null)
        {
            airHaifa = new Airline
            {
                Name = "Air Haifa",
                IataCode = "E2",
                Country = "Israel"
            };

            db.Add(airHaifa);

            db.Add(new AirlineStatistic
            {
                AirlineId = airHaifa.Id,
                AverageRating = 4,
                PositiveSentimentPercent = 76,
                StaffScore = 80,
                ServiceScore = 80,
                EnvironmentalScore = 84,
                AverageCo2 = 94,
                PassengerExperienceScore = 80,
                PriceScore = 81,
                AvailabilityScore = 75,
                OverallScore = 80,
                PositiveTopics = "Convenient Haifa departures, Short routes",
                Complaints = "Limited network"
            });

            await db.SaveChangesAsync();
        }

        var staleFlights = await db.Flights
            .Where(f =>
                (f.Source == "Seed demo" || f.Source == "Israel route demo") &&
                (
                    (f.Origin == "TLV" && f.Destination == "ETM") ||
                    f.Origin == "ETM" ||
                    f.Origin == "HFA"
                ))
            .ToListAsync();

        db.RemoveRange(staleFlights);

        var stalePrices = await db.FlightPrices
            .Where(p =>
                (p.Origin == "TLV" && p.Destination == "ETM") ||
                p.Origin == "ETM" ||
                p.Origin == "HFA")
            .ToListAsync();

        db.RemoveRange(stalePrices);

        await db.SaveChangesAsync();

        var routeSpecs = new[]
        {
            ("TLV", "ETM", "6H", 219m),
            ("TLV", "ETM", "IZ", 249m),
            ("ETM", "TLV", "6H", 219m),
            ("ETM", "TLV", "IZ", 249m),
            ("HFA", "ETM", "E2", 179m),
            ("ETM", "HFA", "E2", 179m),
            ("HFA", "ATH", "E2", 299m),
            ("HFA", "LCA", "E2", 199m)
        };

        var airlines = await db.Airlines
            .Where(a => a.IsActive)
            .ToDictionaryAsync(a => a.IataCode);

        foreach (var (origin, destination, airlineCode, price) in routeSpecs)
        {
            if (!airlines.TryGetValue(airlineCode, out var airline))
                continue;

            db.Flights.Add(new Flight
            {
                EventId = Guid.NewGuid(),
                AirlineId = airline.Id,
                FlightNumber = $"{airline.IataCode}{Random.Shared.Next(40, 899):000}",
                Origin = origin,
                Destination = destination,
                DepartureTime = DateTime.UtcNow.Date.AddDays(1).AddHours(8),
                ArrivalTime = DateTime.UtcNow.Date.AddDays(1).AddHours(9),
                Status = "Scheduled",
                Terminal = 1,
                IsArrival = false,
                Source = "Israel route demo",
                CollectedAt = DateTime.UtcNow
            });

            db.FlightPrices.Add(new FlightPrice
            {
                EventId = Guid.NewGuid(),
                AirlineId = airline.Id,
                Origin = origin,
                Destination = destination,
                Price = price,
                Currency = "ILS",
                TravelDate = DateTime.UtcNow.Date.AddDays(30),
                CollectedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
    }
}