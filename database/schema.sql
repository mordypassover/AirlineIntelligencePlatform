CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261008114947_InitialCreate') THEN
    CREATE TABLE "Airlines" (
        "Id" uuid NOT NULL,
        "Name" character varying(120) NOT NULL,
        "IataCode" character varying(3) NOT NULL,
        "Country" text NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Airlines" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261008114947_InitialCreate') THEN
    CREATE TABLE "AirlineStatisticsHistory" (
        "Id" uuid NOT NULL,
        "AirlineId" uuid NOT NULL,
        "AverageRating" numeric NOT NULL,
        "PositiveSentimentPercent" numeric NOT NULL,
        "StaffScore" numeric NOT NULL,
        "ServiceScore" numeric NOT NULL,
        "PositiveTopics" text NOT NULL,
        "Complaints" text NOT NULL,
        "AveragePrice" numeric NOT NULL,
        "LowestPrice" numeric NOT NULL,
        "HighestPrice" numeric NOT NULL,
        "FlightsPerWeek" integer NOT NULL,
        "DestinationCount" integer NOT NULL,
        "RouteCount" integer NOT NULL,
        "EnvironmentalScore" numeric NOT NULL,
        "AverageCo2" numeric NOT NULL,
        "PassengerExperienceScore" numeric NOT NULL,
        "PriceScore" numeric NOT NULL,
        "AvailabilityScore" numeric NOT NULL,
        "OverallScore" numeric NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "SnapshotAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_AirlineStatisticsHistory" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261008114947_InitialCreate') THEN
    CREATE TABLE "Destinations" (
        "Id" uuid NOT NULL,
        "Code" text NOT NULL,
        "City" text NOT NULL,
        "Country" text NOT NULL,
        CONSTRAINT "PK_Destinations" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261008114947_InitialCreate') THEN
    CREATE TABLE "RawData" (
        "Id" uuid NOT NULL,
        "EventId" uuid NOT NULL,
        "Topic" text NOT NULL,
        "Payload" text NOT NULL,
        "Processed" boolean NOT NULL,
        "Error" text,
        "ReceivedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_RawData" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261008114947_InitialCreate') THEN
    CREATE TABLE "Users" (
        "Id" uuid NOT NULL,
        "Email" character varying(254) NOT NULL,
        "PasswordHash" text NOT NULL,
        "DisplayName" text NOT NULL,
        "Role" text NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261008114947_InitialCreate') THEN
    CREATE TABLE "AirlineStatistics" (
        "AirlineId" uuid NOT NULL,
        "AverageRating" numeric NOT NULL,
        "PositiveSentimentPercent" numeric NOT NULL,
        "StaffScore" numeric NOT NULL,
        "ServiceScore" numeric NOT NULL,
        "PositiveTopics" text NOT NULL,
        "Complaints" text NOT NULL,
        "AveragePrice" numeric NOT NULL,
        "LowestPrice" numeric NOT NULL,
        "HighestPrice" numeric NOT NULL,
        "FlightsPerWeek" integer NOT NULL,
        "DestinationCount" integer NOT NULL,
        "RouteCount" integer NOT NULL,
        "EnvironmentalScore" numeric NOT NULL,
        "AverageCo2" numeric NOT NULL,
        "PassengerExperienceScore" numeric NOT NULL,
        "PriceScore" numeric NOT NULL,
        "AvailabilityScore" numeric NOT NULL,
        "OverallScore" numeric NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_AirlineStatistics" PRIMARY KEY ("AirlineId"),
        CONSTRAINT "FK_AirlineStatistics_Airlines_AirlineId" FOREIGN KEY ("AirlineId") REFERENCES "Airlines" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261008114947_InitialCreate') THEN
    CREATE TABLE "EnvironmentalMeasurements" (
        "Id" uuid NOT NULL,
        "EventId" uuid NOT NULL,
        "AirlineId" uuid NOT NULL,
        "Co2KgPerPassengerKm" numeric NOT NULL,
        "FuelEfficiencyScore" numeric NOT NULL,
        "MeasuredAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_EnvironmentalMeasurements" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_EnvironmentalMeasurements_Airlines_AirlineId" FOREIGN KEY ("AirlineId") REFERENCES "Airlines" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261008114947_InitialCreate') THEN
    CREATE TABLE "FlightPrices" (
        "Id" uuid NOT NULL,
        "EventId" uuid NOT NULL,
        "AirlineId" uuid NOT NULL,
        "Origin" text NOT NULL,
        "Destination" text NOT NULL,
        "Price" numeric(10,2) NOT NULL,
        "Currency" text NOT NULL,
        "TravelDate" timestamp with time zone NOT NULL,
        "CollectedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_FlightPrices" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_FlightPrices_Airlines_AirlineId" FOREIGN KEY ("AirlineId") REFERENCES "Airlines" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261008114947_InitialCreate') THEN
    CREATE TABLE "Flights" (
        "Id" uuid NOT NULL,
        "EventId" uuid NOT NULL,
        "AirlineId" uuid NOT NULL,
        "FlightNumber" text NOT NULL,
        "Origin" text NOT NULL,
        "Destination" text NOT NULL,
        "DepartureTime" timestamp with time zone NOT NULL,
        "ArrivalTime" timestamp with time zone NOT NULL,
        "CollectedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Flights" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Flights_Airlines_AirlineId" FOREIGN KEY ("AirlineId") REFERENCES "Airlines" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261008114947_InitialCreate') THEN
    CREATE TABLE "Reviews" (
        "Id" uuid NOT NULL,
        "EventId" uuid NOT NULL,
        "AirlineId" uuid NOT NULL,
        "Rating" integer NOT NULL,
        "ReviewText" text NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Reviews" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Reviews_Airlines_AirlineId" FOREIGN KEY ("AirlineId") REFERENCES "Airlines" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261008114947_InitialCreate') THEN
    CREATE TABLE "AdminNotes" (
        "Id" uuid NOT NULL,
        "AirlineId" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "Content" text NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_AdminNotes" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AdminNotes_Airlines_AirlineId" FOREIGN KEY ("AirlineId") REFERENCES "Airlines" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_AdminNotes_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261008114947_InitialCreate') THEN
    CREATE TABLE "SavedAirlines" (
        "UserId" uuid NOT NULL,
        "AirlineId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_SavedAirlines" PRIMARY KEY ("UserId", "AirlineId"),
        CONSTRAINT "FK_SavedAirlines_Airlines_AirlineId" FOREIGN KEY ("AirlineId") REFERENCES "Airlines" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_SavedAirlines_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261008114947_InitialCreate') THEN
    CREATE TABLE "SentimentResults" (
        "Id" uuid NOT NULL,
        "EventId" uuid NOT NULL,
        "ReviewId" uuid NOT NULL,
        "AirlineId" uuid NOT NULL,
        "Sentiment" text NOT NULL,
        "Topics" text NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_SentimentResults" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_SentimentResults_Reviews_ReviewId" FOREIGN KEY ("ReviewId") REFERENCES "Reviews" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261008114947_InitialCreate') THEN
    CREATE INDEX "IX_AdminNotes_AirlineId" ON "AdminNotes" ("AirlineId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261008114947_InitialCreate') THEN
    CREATE INDEX "IX_AdminNotes_UserId" ON "AdminNotes" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261008114947_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Airlines_IataCode" ON "Airlines" ("IataCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261008114947_InitialCreate') THEN
    CREATE INDEX "IX_AirlineStatisticsHistory_AirlineId_SnapshotAt" ON "AirlineStatisticsHistory" ("AirlineId", "SnapshotAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261008114947_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Destinations_Code" ON "Destinations" ("Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261008114947_InitialCreate') THEN
    CREATE INDEX "IX_EnvironmentalMeasurements_AirlineId" ON "EnvironmentalMeasurements" ("AirlineId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261008114947_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_EnvironmentalMeasurements_EventId" ON "EnvironmentalMeasurements" ("EventId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261008114947_InitialCreate') THEN
    CREATE INDEX "IX_FlightPrices_AirlineId" ON "FlightPrices" ("AirlineId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261008114947_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_FlightPrices_EventId" ON "FlightPrices" ("EventId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261008114947_InitialCreate') THEN
    CREATE INDEX "IX_Flights_AirlineId" ON "Flights" ("AirlineId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261008114947_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Flights_EventId" ON "Flights" ("EventId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261008114947_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_RawData_EventId" ON "RawData" ("EventId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261008114947_InitialCreate') THEN
    CREATE INDEX "IX_Reviews_AirlineId" ON "Reviews" ("AirlineId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261008114947_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Reviews_EventId" ON "Reviews" ("EventId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261008114947_InitialCreate') THEN
    CREATE INDEX "IX_SavedAirlines_AirlineId" ON "SavedAirlines" ("AirlineId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261008114947_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_SentimentResults_EventId" ON "SentimentResults" ("EventId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261008114947_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_SentimentResults_ReviewId" ON "SentimentResults" ("ReviewId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261008114947_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261008114947_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261008114947_InitialCreate', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261008121649_AddIsraelFlightFields') THEN
    ALTER TABLE "Flights" ADD "IsArrival" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261008121649_AddIsraelFlightFields') THEN
    ALTER TABLE "Flights" ADD "Source" text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261008121649_AddIsraelFlightFields') THEN
    ALTER TABLE "Flights" ADD "Status" text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261008121649_AddIsraelFlightFields') THEN
    ALTER TABLE "Flights" ADD "Terminal" integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261008121649_AddIsraelFlightFields') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261008121649_AddIsraelFlightFields', '8.0.11');
    END IF;
END $EF$;
COMMIT;

