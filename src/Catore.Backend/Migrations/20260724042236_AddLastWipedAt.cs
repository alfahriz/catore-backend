using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catore.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddLastWipedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "lastwipedat",
                table: "profileaccount",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "lastwipedat",
                table: "profileaccount");
        }
    }
}
