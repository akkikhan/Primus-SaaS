using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimusSaaS.Portal.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Modules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ModuleKey = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Modules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModuleVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ModuleId = table.Column<int>(type: "INTEGER", nullable: false),
                    Version = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    IsBreakingChange = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReleaseNotes = table.Column<string>(type: "TEXT", nullable: false),
                    SupportedStacksJson = table.Column<string>(type: "TEXT", nullable: false),
                    ReleasedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModuleVersions_Modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "Modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Applications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OwnerUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Stack = table.Column<int>(type: "INTEGER", nullable: false),
                    PrimusClientId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Applications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Applications_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationModules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApplicationId = table.Column<int>(type: "INTEGER", nullable: false),
                    ModuleId = table.Column<int>(type: "INTEGER", nullable: false),
                    ModuleVersionId = table.Column<int>(type: "INTEGER", nullable: false),
                    ConfigJson = table.Column<string>(type: "TEXT", nullable: false),
                    IntegratedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationModules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationModules_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApplicationModules_ModuleVersions_ModuleVersionId",
                        column: x => x.ModuleVersionId,
                        principalTable: "ModuleVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApplicationModules_Modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "Modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Modules",
                columns: new[] { "Id", "Description", "ModuleKey", "Name" },
                values: new object[] { 1, "Authentication module supporting Local JWT and Azure AD OIDC validation", "identity-validator", "IdentityValidator" });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "PasswordHash", "Role", "UpdatedAt" },
                values: new object[] { 1, new DateTime(2025, 11, 16, 0, 45, 23, 125, DateTimeKind.Utc).AddTicks(7061), "admin@primussaas.com", "$2a$11$YourHashedPasswordHere", 1, new DateTime(2025, 11, 16, 0, 45, 23, 125, DateTimeKind.Utc).AddTicks(7061) });

            migrationBuilder.InsertData(
                table: "ModuleVersions",
                columns: new[] { "Id", "IsBreakingChange", "ModuleId", "ReleaseNotes", "ReleasedAt", "SupportedStacksJson", "Version" },
                values: new object[] { 1, false, 1, "Initial release of IdentityValidator module", new DateTime(2025, 11, 16, 0, 45, 23, 125, DateTimeKind.Utc).AddTicks(7399), "[\"DotNet\",\"NodeJS\"]", "1.0.0" });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationModules_ApplicationId_ModuleId",
                table: "ApplicationModules",
                columns: new[] { "ApplicationId", "ModuleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationModules_ModuleId",
                table: "ApplicationModules",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationModules_ModuleVersionId",
                table: "ApplicationModules",
                column: "ModuleVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_OwnerUserId",
                table: "Applications",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_PrimusClientId",
                table: "Applications",
                column: "PrimusClientId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Modules_ModuleKey",
                table: "Modules",
                column: "ModuleKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Modules_Name",
                table: "Modules",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleVersions_ModuleId_Version",
                table: "ModuleVersions",
                columns: new[] { "ModuleId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationModules");

            migrationBuilder.DropTable(
                name: "Applications");

            migrationBuilder.DropTable(
                name: "ModuleVersions");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Modules");
        }
    }
}
