using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shared.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Airlines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    IataCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Country = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Airlines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AirlineStatisticsHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AirlineId = table.Column<Guid>(type: "uuid", nullable: false),
                    AverageRating = table.Column<decimal>(type: "numeric", nullable: false),
                    PositiveSentimentPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    StaffScore = table.Column<decimal>(type: "numeric", nullable: false),
                    ServiceScore = table.Column<decimal>(type: "numeric", nullable: false),
                    PositiveTopics = table.Column<string>(type: "text", nullable: false),
                    Complaints = table.Column<string>(type: "text", nullable: false),
                    AveragePrice = table.Column<decimal>(type: "numeric", nullable: false),
                    LowestPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    HighestPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    FlightsPerWeek = table.Column<int>(type: "integer", nullable: false),
                    DestinationCount = table.Column<int>(type: "integer", nullable: false),
                    RouteCount = table.Column<int>(type: "integer", nullable: false),
                    EnvironmentalScore = table.Column<decimal>(type: "numeric", nullable: false),
                    AverageCo2 = table.Column<decimal>(type: "numeric", nullable: false),
                    PassengerExperienceScore = table.Column<decimal>(type: "numeric", nullable: false),
                    PriceScore = table.Column<decimal>(type: "numeric", nullable: false),
                    AvailabilityScore = table.Column<decimal>(type: "numeric", nullable: false),
                    OverallScore = table.Column<decimal>(type: "numeric", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SnapshotAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AirlineStatisticsHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Destinations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    City = table.Column<string>(type: "text", nullable: false),
                    Country = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Destinations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RawData",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Topic = table.Column<string>(type: "text", nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    Processed = table.Column<bool>(type: "boolean", nullable: false),
                    Error = table.Column<string>(type: "text", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RawData", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AirlineStatistics",
                columns: table => new
                {
                    AirlineId = table.Column<Guid>(type: "uuid", nullable: false),
                    AverageRating = table.Column<decimal>(type: "numeric", nullable: false),
                    PositiveSentimentPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    StaffScore = table.Column<decimal>(type: "numeric", nullable: false),
                    ServiceScore = table.Column<decimal>(type: "numeric", nullable: false),
                    PositiveTopics = table.Column<string>(type: "text", nullable: false),
                    Complaints = table.Column<string>(type: "text", nullable: false),
                    AveragePrice = table.Column<decimal>(type: "numeric", nullable: false),
                    LowestPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    HighestPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    FlightsPerWeek = table.Column<int>(type: "integer", nullable: false),
                    DestinationCount = table.Column<int>(type: "integer", nullable: false),
                    RouteCount = table.Column<int>(type: "integer", nullable: false),
                    EnvironmentalScore = table.Column<decimal>(type: "numeric", nullable: false),
                    AverageCo2 = table.Column<decimal>(type: "numeric", nullable: false),
                    PassengerExperienceScore = table.Column<decimal>(type: "numeric", nullable: false),
                    PriceScore = table.Column<decimal>(type: "numeric", nullable: false),
                    AvailabilityScore = table.Column<decimal>(type: "numeric", nullable: false),
                    OverallScore = table.Column<decimal>(type: "numeric", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AirlineStatistics", x => x.AirlineId);
                    table.ForeignKey(
                        name: "FK_AirlineStatistics_Airlines_AirlineId",
                        column: x => x.AirlineId,
                        principalTable: "Airlines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EnvironmentalMeasurements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    AirlineId = table.Column<Guid>(type: "uuid", nullable: false),
                    Co2KgPerPassengerKm = table.Column<decimal>(type: "numeric", nullable: false),
                    FuelEfficiencyScore = table.Column<decimal>(type: "numeric", nullable: false),
                    MeasuredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnvironmentalMeasurements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnvironmentalMeasurements_Airlines_AirlineId",
                        column: x => x.AirlineId,
                        principalTable: "Airlines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FlightPrices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    AirlineId = table.Column<Guid>(type: "uuid", nullable: false),
                    Origin = table.Column<string>(type: "text", nullable: false),
                    Destination = table.Column<string>(type: "text", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    TravelDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CollectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlightPrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlightPrices_Airlines_AirlineId",
                        column: x => x.AirlineId,
                        principalTable: "Airlines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Flights",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    AirlineId = table.Column<Guid>(type: "uuid", nullable: false),
                    FlightNumber = table.Column<string>(type: "text", nullable: false),
                    Origin = table.Column<string>(type: "text", nullable: false),
                    Destination = table.Column<string>(type: "text", nullable: false),
                    DepartureTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ArrivalTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CollectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Flights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Flights_Airlines_AirlineId",
                        column: x => x.AirlineId,
                        principalTable: "Airlines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    AirlineId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    ReviewText = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reviews_Airlines_AirlineId",
                        column: x => x.AirlineId,
                        principalTable: "Airlines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdminNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AirlineId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdminNotes_Airlines_AirlineId",
                        column: x => x.AirlineId,
                        principalTable: "Airlines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdminNotes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SavedAirlines",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AirlineId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedAirlines", x => new { x.UserId, x.AirlineId });
                    table.ForeignKey(
                        name: "FK_SavedAirlines_Airlines_AirlineId",
                        column: x => x.AirlineId,
                        principalTable: "Airlines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SavedAirlines_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SentimentResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewId = table.Column<Guid>(type: "uuid", nullable: false),
                    AirlineId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sentiment = table.Column<string>(type: "text", nullable: false),
                    Topics = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SentimentResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SentimentResults_Reviews_ReviewId",
                        column: x => x.ReviewId,
                        principalTable: "Reviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminNotes_AirlineId",
                table: "AdminNotes",
                column: "AirlineId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminNotes_UserId",
                table: "AdminNotes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Airlines_IataCode",
                table: "Airlines",
                column: "IataCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AirlineStatisticsHistory_AirlineId_SnapshotAt",
                table: "AirlineStatisticsHistory",
                columns: new[] { "AirlineId", "SnapshotAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Destinations_Code",
                table: "Destinations",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EnvironmentalMeasurements_AirlineId",
                table: "EnvironmentalMeasurements",
                column: "AirlineId");

            migrationBuilder.CreateIndex(
                name: "IX_EnvironmentalMeasurements_EventId",
                table: "EnvironmentalMeasurements",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FlightPrices_AirlineId",
                table: "FlightPrices",
                column: "AirlineId");

            migrationBuilder.CreateIndex(
                name: "IX_FlightPrices_EventId",
                table: "FlightPrices",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Flights_AirlineId",
                table: "Flights",
                column: "AirlineId");

            migrationBuilder.CreateIndex(
                name: "IX_Flights_EventId",
                table: "Flights",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RawData_EventId",
                table: "RawData",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_AirlineId",
                table: "Reviews",
                column: "AirlineId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_EventId",
                table: "Reviews",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SavedAirlines_AirlineId",
                table: "SavedAirlines",
                column: "AirlineId");

            migrationBuilder.CreateIndex(
                name: "IX_SentimentResults_EventId",
                table: "SentimentResults",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SentimentResults_ReviewId",
                table: "SentimentResults",
                column: "ReviewId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminNotes");

            migrationBuilder.DropTable(
                name: "AirlineStatistics");

            migrationBuilder.DropTable(
                name: "AirlineStatisticsHistory");

            migrationBuilder.DropTable(
                name: "Destinations");

            migrationBuilder.DropTable(
                name: "EnvironmentalMeasurements");

            migrationBuilder.DropTable(
                name: "FlightPrices");

            migrationBuilder.DropTable(
                name: "Flights");

            migrationBuilder.DropTable(
                name: "RawData");

            migrationBuilder.DropTable(
                name: "SavedAirlines");

            migrationBuilder.DropTable(
                name: "SentimentResults");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Reviews");

            migrationBuilder.DropTable(
                name: "Airlines");
        }
    }
}
