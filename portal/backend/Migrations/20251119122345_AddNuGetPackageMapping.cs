using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimusSaaS.Portal.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddNuGetPackageMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ModuleVersions",
                keyColumn: "Id",
                keyValue: 1,
                column: "ReleasedAt",
                value: new DateTime(2025, 11, 19, 12, 23, 44, 966, DateTimeKind.Utc).AddTicks(2294));

            migrationBuilder.UpdateData(
                table: "ModuleVersions",
                keyColumn: "Id",
                keyValue: 2,
                column: "ReleasedAt",
                value: new DateTime(2025, 11, 26, 12, 23, 44, 966, DateTimeKind.Utc).AddTicks(2298));

            migrationBuilder.UpdateData(
                table: "PackageRegistryMappings",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 19, 12, 23, 44, 966, DateTimeKind.Utc).AddTicks(2346));

            migrationBuilder.InsertData(
                table: "PackageRegistryMappings",
                columns: new[] { "Id", "CreatedAt", "ModuleId", "PackageName", "RegistryType" },
                values: new object[] { 2, new DateTime(2025, 11, 19, 12, 23, 44, 966, DateTimeKind.Utc).AddTicks(2348), 1, "PrimusSaaS.Identity.Validator", "nuget" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 19, 12, 23, 44, 966, DateTimeKind.Utc).AddTicks(2044), new DateTime(2025, 11, 19, 12, 23, 44, 966, DateTimeKind.Utc).AddTicks(2045) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PackageRegistryMappings",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.UpdateData(
                table: "ModuleVersions",
                keyColumn: "Id",
                keyValue: 1,
                column: "ReleasedAt",
                value: new DateTime(2025, 11, 19, 12, 12, 4, 529, DateTimeKind.Utc).AddTicks(474));

            migrationBuilder.UpdateData(
                table: "ModuleVersions",
                keyColumn: "Id",
                keyValue: 2,
                column: "ReleasedAt",
                value: new DateTime(2025, 11, 26, 12, 12, 4, 529, DateTimeKind.Utc).AddTicks(478));

            migrationBuilder.UpdateData(
                table: "PackageRegistryMappings",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 19, 12, 12, 4, 529, DateTimeKind.Utc).AddTicks(523));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 19, 12, 12, 4, 529, DateTimeKind.Utc).AddTicks(241), new DateTime(2025, 11, 19, 12, 12, 4, 529, DateTimeKind.Utc).AddTicks(242) });
        }
    }
}
