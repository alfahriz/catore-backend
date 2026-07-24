using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catore.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "isemailverified",
                table: "useraccount",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "verifytoken",
                table: "useraccount",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "verifytokenexpiry",
                table: "useraccount",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isemailverified",
                table: "useraccount");

            migrationBuilder.DropColumn(
                name: "verifytoken",
                table: "useraccount");

            migrationBuilder.DropColumn(
                name: "verifytokenexpiry",
                table: "useraccount");
        }
    }
}
