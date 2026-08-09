using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hika.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase12LoadIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_reviews_created_at_utc",
                table: "reviews",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_payments_status_created_at_utc",
                table: "payments",
                columns: new[] { "status", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_bookings_status_requested_at_utc",
                table: "bookings",
                columns: new[] { "status", "requested_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_reviews_created_at_utc",
                table: "reviews");

            migrationBuilder.DropIndex(
                name: "ix_payments_status_created_at_utc",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "ix_bookings_status_requested_at_utc",
                table: "bookings");
        }
    }
}
