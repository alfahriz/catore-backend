using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catore.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddFreezeGainedDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "laststreakfreezegaineddate",
                table: "freezestate",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "lastwipefreezegaineddate",
                table: "freezestate",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "laststreakfreezegaineddate",
                table: "freezestate");

            migrationBuilder.DropColumn(
                name: "lastwipefreezegaineddate",
                table: "freezestate");
        }
    }
}
