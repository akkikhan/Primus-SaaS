using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimusSaaS.Portal.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixAdminPasswordHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ModuleVersions",
                keyColumn: "Id",
                keyValue: 1,
                column: "ReleasedAt",
                value: new DateTime(2025, 11, 16, 1, 9, 47, 455, DateTimeKind.Utc).AddTicks(2423));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 16, 1, 9, 47, 455, DateTimeKind.Utc).AddTicks(2207), "$2a$11$1cpBqvDSeWEpe8eDpouWDude7DsvSAJ6wtI9Ja4guFVMFPvzZmAuO", new DateTime(2025, 11, 16, 1, 9, 47, 455, DateTimeKind.Utc).AddTicks(2207) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ModuleVersions",
                keyColumn: "Id",
                keyValue: 1,
                column: "ReleasedAt",
                value: new DateTime(2025, 11, 16, 1, 4, 5, 303, DateTimeKind.Utc).AddTicks(2026));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 16, 1, 4, 5, 303, DateTimeKind.Utc).AddTicks(1573), "$2a$11$LQ3h8VzqFpRnPzHvP.qMzO5YJ5gKqW5K1YJ5gKqW5K1YJ5gKqW5K1u", new DateTime(2025, 11, 16, 1, 4, 5, 303, DateTimeKind.Utc).AddTicks(1574) });
        }
    }
}
