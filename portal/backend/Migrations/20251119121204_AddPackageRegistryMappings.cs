using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimusSaaS.Portal.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPackageRegistryMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PackageRegistryMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ModuleId = table.Column<int>(type: "INTEGER", nullable: false),
                    RegistryType = table.Column<string>(type: "TEXT", nullable: false),
                    PackageName = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageRegistryMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackageRegistryMappings_Modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "Modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "ModuleVersions",
                keyColumn: "Id",
                keyValue: 1,
                column: "ReleasedAt",
                value: new DateTime(2025, 11, 19, 12, 12, 4, 529, DateTimeKind.Utc).AddTicks(474));

            migrationBuilder.InsertData(
                table: "ModuleVersions",
                columns: new[] { "Id", "Changelog", "DemoCode", "IsBreakingChange", "ModuleId", "ReleaseNotes", "ReleasedAt", "SupportedStacksJson", "Version" },
                values: new object[] { 2, "Added: in-memory JWKS cache with configurable TTL\nChanged: default audience parsing now trims api:// prefix\nFixed: null reference when openid config is temporarily unavailable", "", false, 1, "Added JWKS caching and improved Azure AD validation defaults", new DateTime(2025, 11, 26, 12, 12, 4, 529, DateTimeKind.Utc).AddTicks(478), "[\"DotNet\",\"NodeJS\"]", "1.1.0" });

            migrationBuilder.InsertData(
                table: "PackageRegistryMappings",
                columns: new[] { "Id", "CreatedAt", "ModuleId", "PackageName", "RegistryType" },
                values: new object[] { 1, new DateTime(2025, 11, 19, 12, 12, 4, 529, DateTimeKind.Utc).AddTicks(523), 1, "primus-identity-validator", "npm" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 19, 12, 12, 4, 529, DateTimeKind.Utc).AddTicks(241), new DateTime(2025, 11, 19, 12, 12, 4, 529, DateTimeKind.Utc).AddTicks(242) });

            migrationBuilder.CreateIndex(
                name: "IX_PackageRegistryMappings_ModuleId",
                table: "PackageRegistryMappings",
                column: "ModuleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PackageRegistryMappings");

            migrationBuilder.DeleteData(
                table: "ModuleVersions",
                keyColumn: "Id",
                keyValue: 2);

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
    }
}
