using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimusSaaS.Portal.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotificationPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    EmailOnNewVersion = table.Column<bool>(type: "INTEGER", nullable: false),
                    EmailOnBreakingChange = table.Column<bool>(type: "INTEGER", nullable: false),
                    EmailOnSecurityUpdate = table.Column<bool>(type: "INTEGER", nullable: false),
                    AdditionalEmails = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_UserId",
                table: "NotificationPreferences",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationPreferences");

            migrationBuilder.UpdateData(
                table: "ModuleVersions",
                keyColumn: "Id",
                keyValue: 1,
                column: "ReleasedAt",
                value: new DateTime(2025, 11, 22, 2, 47, 37, 42, DateTimeKind.Utc).AddTicks(8646));

            migrationBuilder.UpdateData(
                table: "ModuleVersions",
                keyColumn: "Id",
                keyValue: 2,
                column: "ReleasedAt",
                value: new DateTime(2025, 11, 29, 2, 47, 37, 42, DateTimeKind.Utc).AddTicks(8652));

            migrationBuilder.UpdateData(
                table: "PackageRegistryMappings",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 22, 2, 47, 37, 42, DateTimeKind.Utc).AddTicks(8717));

            migrationBuilder.UpdateData(
                table: "PackageRegistryMappings",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 22, 2, 47, 37, 42, DateTimeKind.Utc).AddTicks(8722));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 22, 2, 47, 37, 42, DateTimeKind.Utc).AddTicks(8280), new DateTime(2025, 11, 22, 2, 47, 37, 42, DateTimeKind.Utc).AddTicks(8281) });
        }
    }
}
