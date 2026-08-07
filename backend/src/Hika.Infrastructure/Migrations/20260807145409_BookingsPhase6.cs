using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hika.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BookingsPhase6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bookings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    trip_id = table.Column<Guid>(type: "uuid", nullable: false),
                    passenger_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    boarding_stop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    alighting_stop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seats_requested = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    requested_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    responded_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancellation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    total_price_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_price_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bookings", x => x.id);
                    table.ForeignKey(
                        name: "fk_bookings_trip_stops_alighting_stop_id",
                        column: x => x.alighting_stop_id,
                        principalTable: "trip_stops",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bookings_trip_stops_boarding_stop_id",
                        column: x => x.boarding_stop_id,
                        principalTable: "trip_stops",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bookings_trips_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bookings_user_profiles_passenger_user_id",
                        column: x => x.passenger_user_id,
                        principalTable: "user_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "booking_passengers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    is_account_holder = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_passengers", x => x.id);
                    table.ForeignKey(
                        name: "fk_booking_passengers_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "booking_segments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trip_segment_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_segments", x => x.id);
                    table.ForeignKey(
                        name: "fk_booking_segments_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_booking_segments_trip_segments_trip_segment_id",
                        column: x => x.trip_segment_id,
                        principalTable: "trip_segments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_booking_passengers_booking_id",
                table: "booking_passengers",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "ix_booking_segments_booking_id_trip_segment_id",
                table: "booking_segments",
                columns: new[] { "booking_id", "trip_segment_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_booking_segments_trip_segment_id",
                table: "booking_segments",
                column: "trip_segment_id");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_alighting_stop_id",
                table: "bookings",
                column: "alighting_stop_id");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_boarding_stop_id",
                table: "bookings",
                column: "boarding_stop_id");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_passenger_user_id_status",
                table: "bookings",
                columns: new[] { "passenger_user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_bookings_trip_id",
                table: "bookings",
                column: "trip_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "booking_passengers");

            migrationBuilder.DropTable(
                name: "booking_segments");

            migrationBuilder.DropTable(
                name: "bookings");
        }
    }
}
