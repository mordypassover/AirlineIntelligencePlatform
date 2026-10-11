# Database assets

This folder is intentionally part of the runnable database implementation.

- `schema.sql` is the generated PostgreSQL script for all Entity Framework Core migrations.
- The authoritative migrations are in `backend/Shared/Migrations` and are applied automatically when the API starts.
- Seed data is created by `backend/AirlineApi/Seed.cs` after migrations complete.

The default seed is focused on Israeli aviation: El Al, Israir, Arkia, Sundor and airlines commonly serving Ben Gurion Airport. All seeded prices, reviews and scores are demo data. Live flight-board rows are collected separately from the Israel Airports Authority dataset on data.gov.il.

No manual SQL step is needed. `docker compose up --build` creates PostgreSQL, applies the migrations and loads seed data.
