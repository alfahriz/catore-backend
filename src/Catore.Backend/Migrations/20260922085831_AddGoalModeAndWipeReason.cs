using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catore.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddGoalModeAndWipeReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tdailyrecord_deficitCategory",
                table: "tdailyrecord");

            migrationBuilder.RenameColumn(
                name: "deficitCategory",
                table: "tdailyrecord",
                newName: "calorieCategory");

            migrationBuilder.RenameIndex(
                name: "IX_tdailyrecord_deficitCategory",
                table: "tdailyrecord",
                newName: "IX_tdailyrecord_calorieCategory");

            migrationBuilder.AddColumn<string>(
                name: "wipeReason",
                table: "tweightlog",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "wipeReason",
                table: "tstreak",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "wipeReason",
                table: "tfreeze",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "wipeReason",
                table: "tdailyrecord",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "goalMode",
                table: "mprofile",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_mprofile_goalMode",
                table: "mprofile",
                column: "goalMode");

            migrationBuilder.AddForeignKey(
                name: "FK_mprofile_goalMode",
                table: "mprofile",
                column: "goalMode",
                principalTable: "mparam",
                principalColumn: "paramPK");

            migrationBuilder.AddForeignKey(
                name: "FK_tdailyrecord_calorieCategory",
                table: "tdailyrecord",
                column: "calorieCategory",
                principalTable: "mparam",
                principalColumn: "paramPK");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_mprofile_goalMode",
                table: "mprofile");

            migrationBuilder.DropForeignKey(
                name: "FK_tdailyrecord_calorieCategory",
                table: "tdailyrecord");

            migrationBuilder.DropIndex(
                name: "IX_mprofile_goalMode",
                table: "mprofile");

            migrationBuilder.DropColumn(
                name: "wipeReason",
                table: "tweightlog");

            migrationBuilder.DropColumn(
                name: "wipeReason",
                table: "tstreak");

            migrationBuilder.DropColumn(
                name: "wipeReason",
                table: "tfreeze");

            migrationBuilder.DropColumn(
                name: "wipeReason",
                table: "tdailyrecord");

            migrationBuilder.DropColumn(
                name: "goalMode",
                table: "mprofile");

            migrationBuilder.RenameColumn(
                name: "calorieCategory",
                table: "tdailyrecord",
                newName: "deficitCategory");

            migrationBuilder.RenameIndex(
                name: "IX_tdailyrecord_calorieCategory",
                table: "tdailyrecord",
                newName: "IX_tdailyrecord_deficitCategory");

            migrationBuilder.AddForeignKey(
                name: "FK_tdailyrecord_deficitCategory",
                table: "tdailyrecord",
                column: "deficitCategory",
                principalTable: "mparam",
                principalColumn: "paramPK");
        }
    }
}
