using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PrimusSaaS.Portal.Api.Migrations
{
    /// <inheritdoc />
    public partial class SeedLoggingModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ModuleVersions",
                keyColumn: "Id",
                keyValue: 1,
                column: "ReleasedAt",
                value: new DateTime(2025, 11, 24, 1, 24, 40, 854, DateTimeKind.Utc).AddTicks(4817));

            migrationBuilder.UpdateData(
                table: "ModuleVersions",
                keyColumn: "Id",
                keyValue: 2,
                column: "ReleasedAt",
                value: new DateTime(2025, 12, 1, 1, 24, 40, 854, DateTimeKind.Utc).AddTicks(4821));

            migrationBuilder.InsertData(
                table: "Modules",
                columns: new[] { "Id", "Description", "ModuleKey", "Name" },
                values: new object[] { 2, "Enterprise-ready structured logging with PII masking, file rotation, and context enrichment", "logging", "Logging" });

            migrationBuilder.UpdateData(
                table: "PackageRegistryMappings",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 24, 1, 24, 40, 854, DateTimeKind.Utc).AddTicks(4871));

            migrationBuilder.UpdateData(
                table: "PackageRegistryMappings",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 24, 1, 24, 40, 854, DateTimeKind.Utc).AddTicks(4873));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 24, 1, 24, 40, 854, DateTimeKind.Utc).AddTicks(4558), new DateTime(2025, 11, 24, 1, 24, 40, 854, DateTimeKind.Utc).AddTicks(4559) });

            migrationBuilder.InsertData(
                table: "ModuleVersions",
                columns: new[] { "Id", "Changelog", "DemoCode", "IsBreakingChange", "ModuleId", "ReleaseNotes", "ReleasedAt", "SupportedStacksJson", "Version" },
                values: new object[] { 100, "", "", false, 2, "Initial release of Logging module", new DateTime(2025, 11, 24, 1, 24, 40, 854, DateTimeKind.Utc).AddTicks(4846), "[\"DotNet\",\"NodeJS\"]", "1.0.0" });

            migrationBuilder.InsertData(
                table: "PackageRegistryMappings",
                columns: new[] { "Id", "CreatedAt", "ModuleId", "PackageName", "RegistryType" },
                values: new object[,]
                {
                    { 100, new DateTime(2025, 11, 24, 1, 24, 40, 854, DateTimeKind.Utc).AddTicks(4875), 2, "@primus-saas/logging", "npm" },
                    { 101, new DateTime(2025, 11, 24, 1, 24, 40, 854, DateTimeKind.Utc).AddTicks(4877), 2, "PrimusSaaS.Logging", "nuget" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ModuleVersions",
                keyColumn: "Id",
                keyValue: 100);

            migrationBuilder.DeleteData(
                table: "PackageRegistryMappings",
                keyColumn: "Id",
                keyValue: 100);

            migrationBuilder.DeleteData(
                table: "PackageRegistryMappings",
                keyColumn: "Id",
                keyValue: 101);

            migrationBuilder.DeleteData(
                table: "Modules",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.UpdateData(
                table: "ModuleVersions",
                keyColumn: "Id",
                keyValue: 1,
                column: "ReleasedAt",
                value: new DateTime(2025, 11, 22, 16, 31, 1, 405, DateTimeKind.Utc).AddTicks(6216));

            migrationBuilder.UpdateData(
                table: "ModuleVersions",
                keyColumn: "Id",
                keyValue: 2,
                column: "ReleasedAt",
                value: new DateTime(2025, 11, 29, 16, 31, 1, 405, DateTimeKind.Utc).AddTicks(6219));

            migrationBuilder.UpdateData(
                table: "PackageRegistryMappings",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 22, 16, 31, 1, 405, DateTimeKind.Utc).AddTicks(6268));

            migrationBuilder.UpdateData(
                table: "PackageRegistryMappings",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 22, 16, 31, 1, 405, DateTimeKind.Utc).AddTicks(6270));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 22, 16, 31, 1, 405, DateTimeKind.Utc).AddTicks(5987), new DateTime(2025, 11, 22, 16, 31, 1, 405, DateTimeKind.Utc).AddTicks(5987) });
        }
    }
}
