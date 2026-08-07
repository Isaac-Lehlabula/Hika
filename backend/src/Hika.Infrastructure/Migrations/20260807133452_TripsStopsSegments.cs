using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hika.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TripsStopsSegments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "locations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    province = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    latitude = table.Column<decimal>(type: "numeric(9,6)", nullable: true),
                    longitude = table.Column<decimal>(type: "numeric(9,6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_locations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "trips",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    driver_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    departure_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    total_seats_offered = table.Column<int>(type: "integer", nullable: false),
                    luggage_allowance = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    price_per_seat_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    price_per_seat_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_trips", x => x.id);
                    table.ForeignKey(
                        name: "fk_trips_driver_profiles_driver_profile_id",
                        column: x => x.driver_profile_id,
                        principalTable: "driver_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_trips_vehicles_vehicle_id",
                        column: x => x.vehicle_id,
                        principalTable: "vehicles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "trip_stops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    trip_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    raw_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    province = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    estimated_arrival_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    estimated_departure_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_trip_stops", x => x.id);
                    table.ForeignKey(
                        name: "fk_trip_stops_locations_location_id",
                        column: x => x.location_id,
                        principalTable: "locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_trip_stops_trips_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trip_segments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    trip_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_stop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_stop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seats_available = table.Column<int>(type: "integer", nullable: false),
                    price_override_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    price_override_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_trip_segments", x => x.id);
                    table.ForeignKey(
                        name: "fk_trip_segments_trip_stops_from_stop_id",
                        column: x => x.from_stop_id,
                        principalTable: "trip_stops",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_trip_segments_trip_stops_to_stop_id",
                        column: x => x.to_stop_id,
                        principalTable: "trip_stops",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_trip_segments_trips_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_locations_name",
                table: "locations",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_trip_segments_from_stop_id",
                table: "trip_segments",
                column: "from_stop_id");

            migrationBuilder.CreateIndex(
                name: "ix_trip_segments_to_stop_id",
                table: "trip_segments",
                column: "to_stop_id");

            migrationBuilder.CreateIndex(
                name: "ix_trip_segments_trip_id_from_stop_id_to_stop_id",
                table: "trip_segments",
                columns: new[] { "trip_id", "from_stop_id", "to_stop_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_trip_stops_location_id",
                table: "trip_stops",
                column: "location_id");

            migrationBuilder.CreateIndex(
                name: "ix_trip_stops_trip_id_sequence",
                table: "trip_stops",
                columns: new[] { "trip_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_trips_driver_profile_id",
                table: "trips",
                column: "driver_profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_trips_status_departure_at_utc",
                table: "trips",
                columns: new[] { "status", "departure_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_trips_vehicle_id",
                table: "trips",
                column: "vehicle_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "trip_segments");

            migrationBuilder.DropTable(
                name: "trip_stops");

            migrationBuilder.DropTable(
                name: "locations");

            migrationBuilder.DropTable(
                name: "trips");
        }
    }
}
