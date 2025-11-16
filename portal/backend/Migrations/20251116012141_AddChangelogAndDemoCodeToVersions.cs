using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimusSaaS.Portal.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddChangelogAndDemoCodeToVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Changelog",
                table: "ModuleVersions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DemoCode",
                table: "ModuleVersions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "ModuleVersions",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Changelog", "DemoCode", "ReleasedAt" },
                values: new object[] { "", "", new DateTime(2025, 11, 16, 1, 21, 41, 678, DateTimeKind.Utc).AddTicks(2471) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 16, 1, 21, 41, 678, DateTimeKind.Utc).AddTicks(2259), new DateTime(2025, 11, 16, 1, 21, 41, 678, DateTimeKind.Utc).AddTicks(2259) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Changelog",
                table: "ModuleVersions");

            migrationBuilder.DropColumn(
                name: "DemoCode",
                table: "ModuleVersions");

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
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 16, 1, 9, 47, 455, DateTimeKind.Utc).AddTicks(2207), new DateTime(2025, 11, 16, 1, 9, 47, 455, DateTimeKind.Utc).AddTicks(2207) });
        }
    }
}
