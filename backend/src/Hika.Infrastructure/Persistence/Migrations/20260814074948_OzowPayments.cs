using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hika.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OzowPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "redirect_url",
                table: "payments",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "redirect_url",
                table: "payments");
        }
    }
}
