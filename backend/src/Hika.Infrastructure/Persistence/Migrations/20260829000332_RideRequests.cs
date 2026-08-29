using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hika.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RideRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ride_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rider_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    origin_raw_text = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    destination_raw_text = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    travel_date = table.Column<DateOnly>(type: "date", nullable: false),
                    seats_needed = table.Column<int>(type: "integer", nullable: false),
                    proposed_price_per_seat = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    claimed_by_driver_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    claimed_booking_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ride_requests", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ride_requests_rider_user_id",
                table: "ride_requests",
                column: "rider_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_ride_requests_status_travel_date",
                table: "ride_requests",
                columns: new[] { "status", "travel_date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ride_requests");
        }
    }
}
