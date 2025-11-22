using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimusSaaS.Portal.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAppUsersAndSigningKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JwtSigningKey",
                table: "Applications",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AppUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApplicationId = table.Column<int>(type: "INTEGER", nullable: false),
                    Username = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppUsers_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_ApplicationId_Username",
                table: "AppUsers",
                columns: new[] { "ApplicationId", "Username" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppUsers");

            migrationBuilder.DropColumn(
                name: "JwtSigningKey",
                table: "Applications");

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
    }
}
