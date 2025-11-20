using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimusSaaS.Portal.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationClientSecret : Migration
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
        }
    }
}
