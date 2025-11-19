using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimusSaaS.Portal.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddWebhookRequestAuditTrail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WebhookRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Endpoint = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    RegistryType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Payload = table.Column<string>(type: "TEXT", nullable: false),
                    Signature = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 45, nullable: false),
                    StatusCode = table.Column<int>(type: "INTEGER", nullable: false),
                    ResponseBody = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProcessingTimeMs = table.Column<int>(type: "INTEGER", nullable: false),
                    SignatureValid = table.Column<bool>(type: "INTEGER", nullable: false),
                    EventType = table.Column<string>(type: "TEXT", nullable: true),
                    PackageName = table.Column<string>(type: "TEXT", nullable: true),
                    PackageVersion = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookRequests", x => x.Id);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_WebhookRequests_CreatedAt",
                table: "WebhookRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookRequests_RegistryType_PackageName",
                table: "WebhookRequests",
                columns: new[] { "RegistryType", "PackageName" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WebhookRequests");

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

            migrationBuilder.UpdateData(
                table: "PackageRegistryMappings",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 19, 12, 23, 44, 966, DateTimeKind.Utc).AddTicks(2348));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 19, 12, 23, 44, 966, DateTimeKind.Utc).AddTicks(2044), new DateTime(2025, 11, 19, 12, 23, 44, 966, DateTimeKind.Utc).AddTicks(2045) });
        }
    }
}
