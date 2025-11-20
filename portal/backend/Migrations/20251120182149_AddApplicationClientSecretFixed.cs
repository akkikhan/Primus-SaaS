using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimusSaaS.Portal.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationClientSecretFixed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientSecretHash",
                table: "Applications",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ClientSecretLastRotatedAt",
                table: "Applications",
                type: "TEXT",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "ModuleVersions",
                keyColumn: "Id",
                keyValue: 1,
                column: "ReleasedAt",
                value: new DateTime(2025, 11, 20, 18, 21, 49, 310, DateTimeKind.Utc).AddTicks(493));

            migrationBuilder.UpdateData(
                table: "ModuleVersions",
                keyColumn: "Id",
                keyValue: 2,
                column: "ReleasedAt",
                value: new DateTime(2025, 11, 27, 18, 21, 49, 310, DateTimeKind.Utc).AddTicks(499));

            migrationBuilder.UpdateData(
                table: "PackageRegistryMappings",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 20, 18, 21, 49, 310, DateTimeKind.Utc).AddTicks(585));

            migrationBuilder.UpdateData(
                table: "PackageRegistryMappings",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 20, 18, 21, 49, 310, DateTimeKind.Utc).AddTicks(589));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 20, 18, 21, 49, 309, DateTimeKind.Utc).AddTicks(9627), new DateTime(2025, 11, 20, 18, 21, 49, 309, DateTimeKind.Utc).AddTicks(9628) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClientSecretHash",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "ClientSecretLastRotatedAt",
                table: "Applications");

            migrationBuilder.UpdateData(
                table: "ModuleVersions",
                keyColumn: "Id",
                keyValue: 1,
                column: "ReleasedAt",
                value: new DateTime(2025, 11, 19, 12, 52, 41, 252, DateTimeKind.Utc).AddTicks(7270));

            migrationBuilder.UpdateData(
                table: "ModuleVersions",
                keyColumn: "Id",
                keyValue: 2,
                column: "ReleasedAt",
                value: new DateTime(2025, 11, 26, 12, 52, 41, 252, DateTimeKind.Utc).AddTicks(7276));

            migrationBuilder.UpdateData(
                table: "PackageRegistryMappings",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 19, 12, 52, 41, 252, DateTimeKind.Utc).AddTicks(7334));

            migrationBuilder.UpdateData(
                table: "PackageRegistryMappings",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 19, 12, 52, 41, 252, DateTimeKind.Utc).AddTicks(7337));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 19, 12, 52, 41, 252, DateTimeKind.Utc).AddTicks(6874), new DateTime(2025, 11, 19, 12, 52, 41, 252, DateTimeKind.Utc).AddTicks(6875) });
        }
    }
}
