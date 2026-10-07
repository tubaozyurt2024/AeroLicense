using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AeroLicense.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFlightLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "flight_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PilotId = table.Column<Guid>(type: "uuid", nullable: false),
                    LicenseId = table.Column<Guid>(type: "uuid", nullable: false),
                    AirlineId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FlightNumber = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    DepartureAirport = table.Column<string>(type: "character(4)", fixedLength: true, maxLength: 4, nullable: false),
                    ArrivalAirport = table.Column<string>(type: "character(4)", fixedLength: true, maxLength: 4, nullable: false),
                    DepartureAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ArrivalAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_flight_logs", x => x.Id);
                    table.CheckConstraint("ck_flight_logs_departure_before_arrival", "\"DepartureAtUtc\" < \"ArrivalAtUtc\"");
                    table.CheckConstraint("ck_flight_logs_duration_positive", "\"DurationMinutes\" > 0");
                    table.ForeignKey(
                        name: "FK_flight_logs_licenses_LicenseId",
                        column: x => x.LicenseId,
                        principalTable: "licenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_flight_logs_organizations_AirlineId",
                        column: x => x.AirlineId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_flight_logs_users_PilotId",
                        column: x => x.PilotId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_flight_logs_users_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "idempotency_records",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RequestHash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idempotency_records", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_flight_logs_AirlineId_RecordedAtUtc",
                table: "flight_logs",
                columns: new[] { "AirlineId", "RecordedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_flight_logs_LicenseId",
                table: "flight_logs",
                column: "LicenseId");

            migrationBuilder.CreateIndex(
                name: "IX_flight_logs_PilotId_DepartureAtUtc",
                table: "flight_logs",
                columns: new[] { "PilotId", "DepartureAtUtc" })
                .Annotation("Npgsql:IndexInclude", new[] { "DurationMinutes", "ArrivalAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_flight_logs_RecordedByUserId",
                table: "flight_logs",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_idempotency_records_ClientId_Key",
                table: "idempotency_records",
                columns: new[] { "ClientId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_idempotency_records_CreatedAtUtc",
                table: "idempotency_records",
                column: "CreatedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "flight_logs");

            migrationBuilder.DropTable(
                name: "idempotency_records");
        }
    }
}
