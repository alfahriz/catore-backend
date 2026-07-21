using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catore.Backend.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "consumptionentry",
                columns: table => new
                {
                    consumptionentrypk = table.Column<Guid>(type: "uuid", nullable: false),
                    userid = table.Column<Guid>(type: "uuid", nullable: false),
                    entrydate = table.Column<DateOnly>(type: "date", nullable: false),
                    mealtype = table.Column<string>(type: "text", nullable: false),
                    foodname = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    calories = table.Column<int>(type: "integer", nullable: false),
                    entrytimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    isdeleted = table.Column<bool>(type: "boolean", nullable: false),
                    isdeletedon = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_consumptionentry", x => x.consumptionentrypk);
                });

            migrationBuilder.CreateTable(
                name: "dailyrecord",
                columns: table => new
                {
                    dailyrecordpk = table.Column<Guid>(type: "uuid", nullable: false),
                    userid = table.Column<Guid>(type: "uuid", nullable: false),
                    recorddate = table.Column<DateOnly>(type: "date", nullable: false),
                    deficitcategory = table.Column<string>(type: "text", nullable: false),
                    patoday = table.Column<bool>(type: "boolean", nullable: false),
                    effectivetdee = table.Column<decimal>(type: "numeric", nullable: false),
                    effectivelimit = table.Column<decimal>(type: "numeric", nullable: false),
                    createdvia = table.Column<string>(type: "text", nullable: false),
                    isfrozen = table.Column<bool>(type: "boolean", nullable: false),
                    lastupdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    isdeleted = table.Column<bool>(type: "boolean", nullable: false),
                    isdeletedon = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dailyrecord", x => x.dailyrecordpk);
                });

            migrationBuilder.CreateTable(
                name: "freezestate",
                columns: table => new
                {
                    freezestatepk = table.Column<Guid>(type: "uuid", nullable: false),
                    userid = table.Column<Guid>(type: "uuid", nullable: false),
                    streakfreezecount = table.Column<int>(type: "integer", nullable: false),
                    wipefreezecount = table.Column<int>(type: "integer", nullable: false),
                    dayssincelaststreakfreeze = table.Column<int>(type: "integer", nullable: false),
                    dayssincelastwipefreeze = table.Column<int>(type: "integer", nullable: false),
                    lastupdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_freezestate", x => x.freezestatepk);
                });

            migrationBuilder.CreateTable(
                name: "notificationsubscription",
                columns: table => new
                {
                    notificationsubscriptionpk = table.Column<Guid>(type: "uuid", nullable: false),
                    userid = table.Column<Guid>(type: "uuid", nullable: false),
                    fcmtoken = table.Column<string>(type: "text", nullable: true),
                    lastupdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notificationsubscription", x => x.notificationsubscriptionpk);
                });

            migrationBuilder.CreateTable(
                name: "profile",
                columns: table => new
                {
                    profilepk = table.Column<Guid>(type: "uuid", nullable: false),
                    userid = table.Column<Guid>(type: "uuid", nullable: false),
                    height = table.Column<decimal>(type: "numeric", nullable: false),
                    weightcurrent = table.Column<decimal>(type: "numeric", nullable: false),
                    age = table.Column<int>(type: "integer", nullable: false),
                    gender = table.Column<string>(type: "text", nullable: false),
                    displayname = table.Column<string>(type: "text", nullable: false),
                    baselineactivitylevel = table.Column<string>(type: "text", nullable: false),
                    goalweight = table.Column<decimal>(type: "numeric", nullable: true),
                    goalweightismanual = table.Column<bool>(type: "boolean", nullable: false),
                    metricpreference = table.Column<string>(type: "text", nullable: false),
                    timezone = table.Column<string>(type: "text", nullable: false),
                    lastupdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    isdeleted = table.Column<bool>(type: "boolean", nullable: false),
                    isupgraded = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profile", x => x.profilepk);
                });

            migrationBuilder.CreateTable(
                name: "streakstate",
                columns: table => new
                {
                    streakstatepk = table.Column<Guid>(type: "uuid", nullable: false),
                    userid = table.Column<Guid>(type: "uuid", nullable: false),
                    currentstreakcount = table.Column<int>(type: "integer", nullable: false),
                    lastloggeddate = table.Column<DateOnly>(type: "date", nullable: true),
                    streakisfrozen = table.Column<bool>(type: "boolean", nullable: false),
                    lastupdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_streakstate", x => x.streakstatepk);
                });

            migrationBuilder.CreateTable(
                name: "useraccount",
                columns: table => new
                {
                    useraccountpk = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    passwordhash = table.Column<string>(type: "text", nullable: false),
                    refreshtoken = table.Column<string>(type: "text", nullable: true),
                    refreshtokenexpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    activesessionid = table.Column<Guid>(type: "uuid", nullable: true),
                    resettoken = table.Column<string>(type: "text", nullable: true),
                    resettokenexpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    lastupdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_useraccount", x => x.useraccountpk);
                });

            migrationBuilder.CreateTable(
                name: "weightlog",
                columns: table => new
                {
                    weightlogpk = table.Column<Guid>(type: "uuid", nullable: false),
                    userid = table.Column<Guid>(type: "uuid", nullable: false),
                    weightvalue = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    loggedat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updatedat = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    isdeleted = table.Column<bool>(type: "boolean", nullable: false),
                    isdeletedon = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weightlog", x => x.weightlogpk);
                });

            migrationBuilder.CreateIndex(
                name: "IX_dailyrecord_userid_recorddate",
                table: "dailyrecord",
                columns: new[] { "userid", "recorddate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_freezestate_userid",
                table: "freezestate",
                column: "userid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notificationsubscription_userid",
                table: "notificationsubscription",
                column: "userid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_profile_userid",
                table: "profile",
                column: "userid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_streakstate_userid",
                table: "streakstate",
                column: "userid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_useraccount_email",
                table: "useraccount",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "consumptionentry");

            migrationBuilder.DropTable(
                name: "dailyrecord");

            migrationBuilder.DropTable(
                name: "freezestate");

            migrationBuilder.DropTable(
                name: "notificationsubscription");

            migrationBuilder.DropTable(
                name: "profile");

            migrationBuilder.DropTable(
                name: "streakstate");

            migrationBuilder.DropTable(
                name: "useraccount");

            migrationBuilder.DropTable(
                name: "weightlog");
        }
    }
}
