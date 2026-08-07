using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hika.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdminPhase11 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_admin",
                table: "user_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_suspended",
                table: "user_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "suspended_at_utc",
                table: "user_profiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "suspension_reason",
                table: "user_profiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    admin_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    details = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "platform_fee_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rate = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by_admin_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_fee_settings", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_admin_user_id",
                table: "audit_logs",
                column: "admin_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_created_at_utc",
                table: "audit_logs",
                column: "created_at_utc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "platform_fee_settings");

            migrationBuilder.DropColumn(
                name: "is_admin",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "is_suspended",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "suspended_at_utc",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "suspension_reason",
                table: "user_profiles");
        }
    }
}
