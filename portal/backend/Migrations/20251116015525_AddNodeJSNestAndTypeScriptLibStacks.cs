using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimusSaaS.Portal.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddNodeJSNestAndTypeScriptLibStacks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ModuleVersions",
                keyColumn: "Id",
                keyValue: 1,
                column: "ReleasedAt",
                value: new DateTime(2025, 11, 16, 1, 55, 25, 313, DateTimeKind.Utc).AddTicks(3702));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 16, 1, 55, 25, 313, DateTimeKind.Utc).AddTicks(3092), new DateTime(2025, 11, 16, 1, 55, 25, 313, DateTimeKind.Utc).AddTicks(3093) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ModuleVersions",
                keyColumn: "Id",
                keyValue: 1,
                column: "ReleasedAt",
                value: new DateTime(2025, 11, 16, 1, 21, 41, 678, DateTimeKind.Utc).AddTicks(2471));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 16, 1, 21, 41, 678, DateTimeKind.Utc).AddTicks(2259), new DateTime(2025, 11, 16, 1, 21, 41, 678, DateTimeKind.Utc).AddTicks(2259) });
        }
    }
}
