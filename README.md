# AirScope — Airline Intelligence Platform

AirScope is an Israel-focused airline-information platform. It combines the live Ben Gurion Airport flight board, route comparisons from Ben Gurion (TLV), Ramon/Eilat (ETM), and Haifa (HFA), passenger reviews, deterministic sentiment, fare observations, environmental measurements, historical trends, accounts, saved airlines, and an administrator workspace. Reviews, fares, environmental values and scores are demo data; live flight-board records come from the Israel Airports Authority via data.gov.il.

## Run the complete platform

```bash
docker compose up --build
```

Then open:

- Frontend: http://localhost:3000
- API: http://localhost:8080
- Swagger: http://localhost:8080/swagger

The default development administrator is `admin@airline.local` / `ChangeMe123!`. Change these values through `.env` before using the app outside local development. Copy `.env.example` to `.env` to customize configuration.

## Architecture

```text
IAA data.gov.il + demo collectors → Kafka → processing/analytics → PostgreSQL
                                                        ↓
Browser ← Nginx frontend proxy ← REST API ← Redis statistics cache
```

The browser talks only to controller-based REST endpoints. Collectors publish JSON events to Kafka. The flight collector calls the key-free Israel Airports Authority CKAN feed and falls back gracefully to seeded Israel route data if the source is unavailable. The sentiment service enriches review events. The analytics service stores idempotent records, recomputes current statistics, and appends historical snapshots.

## Services

| Service | Purpose |
|---|---|
| `frontend` | Responsive vanilla HTML/CSS/JS application served by Nginx |
| `api` | Controller-based .NET 8 REST API, JWT accounts, user and admin operations |
| `review-collector` | Periodic deterministic passenger review events |
| `sentiment-service` | Word-based Positive/Neutral/Negative classification |
| `price-collector` | Periodic mock fare observations |
| `flight-collector` | Live Ben Gurion arrivals/departures from data.gov.il |
| `analytics-service` | Idempotent persistence, aggregation, scoring and history |
| `postgres` | Durable relational data |
| `kafka` | KRaft-mode asynchronous event pipeline |
| `redis` | Airline-statistics response cache |

## Repository structure

```text
backend/AirlineApi        HTTP API, seed data, authentication
backend/Shared            EF Core model, events, scoring, sentiment
backend/ReviewCollector   Review publisher
backend/SentimentService  Review consumer and sentiment publisher
backend/PriceCollector    Price publisher
backend/FlightCollector   Flight/environment publisher
backend/AnalyticsService  Multi-topic analytics consumer
backend/Tests             Focused business/security tests
frontend                  Browser application and Nginx proxy
database                  Complete SQL schema plus migration notes
```

## Data and scoring

On first startup the API applies the included Entity Framework Core migration and seeds seven well-known airlines, destinations, reviews, sentiments, prices, flights, environmental readings, six months of historical statistics, and the administrator. The overall score is isolated in `OverallScoreCalculator` and uses Passenger 40%, Price 25%, Environment 20%, Availability 15%.

Duplicate event IDs have unique database indexes. Consumers catch malformed JSON and database conflicts, log them, and continue. Redis failure is non-fatal: statistics fall back to PostgreSQL.

## Kafka topics

- `airline.reviews`
- `airline.sentiment`
- `airline.prices`
- `airline.flights`
- `airline.environment`

Kafka auto-creates topics in the local Compose environment. Collection intervals are configurable and intentionally short for demonstration.

## API summary

Public endpoints cover `/api/airlines`, airline detail/statistics/reviews/prices/flights/environment, `/api/rankings`, and `/api/destinations`. The Israel board is available at `/api/flights/israel/live`, with `/api/flights/israel` exposing persisted Israel-route records. Authentication is under `/api/auth`. Authenticated user and saved-airline operations are under `/api/users/me`. Administrator airline CRUD, private notes, and dashboard metrics are under `/api/admin` and require the `ADMIN` role. Swagger contains the precise request/response surface.

`database/schema.sql` is a non-empty, idempotent PostgreSQL script generated from all included EF Core migrations. The application applies those migrations automatically during startup; the SQL file is also available for manual provisioning and inspection.

JWT passwords are BCrypt hashes; plaintext passwords are never stored. Logout invalidates the browser session by removing the bearer token. Use HTTPS and rotate the JWT secret in any non-local deployment.

## Configuration

`.env.example` documents PostgreSQL credentials, JWT settings, the seeded administrator, collector intervals and Redis cache TTL. Internal connection strings and Kafka/Redis hostnames are composed from environment variables in `docker-compose.yml`; secrets are not compiled into the app.

## Tests

```bash
dotnet test AirlineIntelligencePlatform.slnx
```

The test project covers weighted scoring, score bounds, deterministic sentiment, secure password hashing, and administrator role logic.

## Useful Docker commands

```bash
docker compose ps
docker compose logs -f api analytics-service
docker compose down
docker compose down -v   # also removes local demo data
```
