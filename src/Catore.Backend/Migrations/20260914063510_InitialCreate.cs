using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

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
                name: "mparam",
                columns: table => new
                {
                    paramPK = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    paramType = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<int>(type: "integer", nullable: false),
                    createdOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    createdBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mparam", x => x.paramPK);
                });

            migrationBuilder.CreateTable(
                name: "muser",
                columns: table => new
                {
                    userPk = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    email = table.Column<string>(type: "text", nullable: false),
                    password = table.Column<string>(type: "text", nullable: false),
                    jwtRefreshToken = table.Column<string>(type: "text", nullable: true),
                    jwtRefreshTokenExpiredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    resetToken = table.Column<string>(type: "text", nullable: true),
                    resetTokenExpiredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    isEmailVerif = table.Column<bool>(type: "boolean", nullable: false),
                    emailVerifiedToken = table.Column<string>(type: "text", nullable: true),
                    emailVerifiedTokenExpiredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    createdOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    createdBy = table.Column<long>(type: "bigint", nullable: true),
                    modifiedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_muser", x => x.userPk);
                });

            migrationBuilder.CreateTable(
                name: "tnotification",
                columns: table => new
                {
                    notificationPk = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userId = table.Column<long>(type: "bigint", nullable: false),
                    fcmToken = table.Column<string>(type: "text", nullable: true),
                    modifiedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tnotification", x => x.notificationPk);
                });

            migrationBuilder.CreateTable(
                name: "mparamnotif",
                columns: table => new
                {
                    notifPk = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    key = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<long>(type: "bigint", nullable: true),
                    title = table.Column<string>(type: "text", nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    isActive = table.Column<bool>(type: "boolean", nullable: false),
                    createdOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modifiedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mparamnotif", x => x.notifPk);
                    table.ForeignKey(
                        name: "FK_mparamnotif_type",
                        column: x => x.type,
                        principalTable: "mparam",
                        principalColumn: "paramPK");
                });

            migrationBuilder.CreateTable(
                name: "mconsumption",
                columns: table => new
                {
                    consumptionPk = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    calories = table.Column<int>(type: "integer", nullable: false),
                    createdOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    createdBy = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mconsumption", x => x.consumptionPk);
                    table.ForeignKey(
                        name: "FK_mconsumption_createdBy",
                        column: x => x.createdBy,
                        principalTable: "muser",
                        principalColumn: "userPk",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mprofile",
                columns: table => new
                {
                    profilePK = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userId = table.Column<long>(type: "bigint", nullable: false),
                    height = table.Column<decimal>(type: "numeric", nullable: false),
                    weight = table.Column<decimal>(type: "numeric", nullable: false),
                    age = table.Column<int>(type: "integer", nullable: false),
                    gender = table.Column<long>(type: "bigint", nullable: true),
                    name = table.Column<string>(type: "text", nullable: false),
                    baseActLevel = table.Column<long>(type: "bigint", nullable: true),
                    goalWeight = table.Column<decimal>(type: "numeric", nullable: true),
                    isRecomendGoalUsed = table.Column<bool>(type: "boolean", nullable: false),
                    metricParam = table.Column<long>(type: "bigint", nullable: true),
                    timezone = table.Column<string>(type: "text", nullable: false),
                    modifiedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modifiedBy = table.Column<string>(type: "text", nullable: true),
                    isActive = table.Column<bool>(type: "boolean", nullable: false),
                    isUpgraded = table.Column<bool>(type: "boolean", nullable: false),
                    lastWipeOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    createdOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    createdBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mprofile", x => x.profilePK);
                    table.ForeignKey(
                        name: "FK_mprofile_baseActLevel",
                        column: x => x.baseActLevel,
                        principalTable: "mparam",
                        principalColumn: "paramPK");
                    table.ForeignKey(
                        name: "FK_mprofile_gender",
                        column: x => x.gender,
                        principalTable: "mparam",
                        principalColumn: "paramPK");
                    table.ForeignKey(
                        name: "FK_mprofile_metricParam",
                        column: x => x.metricParam,
                        principalTable: "mparam",
                        principalColumn: "paramPK");
                    table.ForeignKey(
                        name: "FK_mprofile_muser",
                        column: x => x.userId,
                        principalTable: "muser",
                        principalColumn: "userPk",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tdailyrecord",
                columns: table => new
                {
                    dailyRecordPk = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userId = table.Column<long>(type: "bigint", nullable: false),
                    recordDate = table.Column<DateOnly>(type: "date", nullable: false),
                    deficitCategory = table.Column<long>(type: "bigint", nullable: true),
                    paToday = table.Column<bool>(type: "boolean", nullable: false),
                    effectiveTdee = table.Column<decimal>(type: "numeric", nullable: false),
                    effectiveLimit = table.Column<decimal>(type: "numeric", nullable: false),
                    actualCalories = table.Column<int>(type: "integer", nullable: false),
                    createdVia = table.Column<long>(type: "bigint", nullable: true),
                    isFrozen = table.Column<bool>(type: "boolean", nullable: false),
                    modifiedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    isDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    isDeletedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tdailyrecord", x => x.dailyRecordPk);
                    table.ForeignKey(
                        name: "FK_tdailyrecord_createdVia",
                        column: x => x.createdVia,
                        principalTable: "mparam",
                        principalColumn: "paramPK");
                    table.ForeignKey(
                        name: "FK_tdailyrecord_deficitCategory",
                        column: x => x.deficitCategory,
                        principalTable: "mparam",
                        principalColumn: "paramPK");
                    table.ForeignKey(
                        name: "FK_tdailyrecord_muser",
                        column: x => x.userId,
                        principalTable: "muser",
                        principalColumn: "userPk",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tfreeze",
                columns: table => new
                {
                    freezePk = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userId = table.Column<long>(type: "bigint", nullable: false),
                    streakFreezeCount = table.Column<int>(type: "integer", nullable: false),
                    wipeFreezeCount = table.Column<int>(type: "integer", nullable: false),
                    daysSinceLastStreakFreeze = table.Column<int>(type: "integer", nullable: false),
                    daysSinceLastWipeFreeze = table.Column<int>(type: "integer", nullable: false),
                    lastStreakFreezeGainedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    lastWipeFreezeGainedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    modifiedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tfreeze", x => x.freezePk);
                    table.ForeignKey(
                        name: "FK_tfreeze_muser",
                        column: x => x.userId,
                        principalTable: "muser",
                        principalColumn: "userPk",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tstreak",
                columns: table => new
                {
                    streakPk = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userId = table.Column<long>(type: "bigint", nullable: false),
                    currentStreak = table.Column<int>(type: "integer", nullable: false),
                    lastLoggedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    isStreakFrozen = table.Column<bool>(type: "boolean", nullable: false),
                    modifiedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tstreak", x => x.streakPk);
                    table.ForeignKey(
                        name: "FK_tstreak_muser",
                        column: x => x.userId,
                        principalTable: "muser",
                        principalColumn: "userPk",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tweightlog",
                columns: table => new
                {
                    weightLogPk = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userId = table.Column<long>(type: "bigint", nullable: false),
                    checkpointDate = table.Column<DateOnly>(type: "date", nullable: false),
                    weight = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    createdOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modifiedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    isDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    isDeletedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tweightlog", x => x.weightLogPk);
                    table.ForeignKey(
                        name: "FK_tweightlog_muser",
                        column: x => x.userId,
                        principalTable: "muser",
                        principalColumn: "userPk",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tconsumption",
                columns: table => new
                {
                    entryPk = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userId = table.Column<long>(type: "bigint", nullable: false),
                    consumptionId = table.Column<long>(type: "bigint", nullable: false),
                    mealType = table.Column<long>(type: "bigint", nullable: true),
                    entryTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    createdOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    isDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    isDeletedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tconsumption", x => x.entryPk);
                    table.ForeignKey(
                        name: "FK_tconsumption_mconsumption",
                        column: x => x.consumptionId,
                        principalTable: "mconsumption",
                        principalColumn: "consumptionPk",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tconsumption_mealType",
                        column: x => x.mealType,
                        principalTable: "mparam",
                        principalColumn: "paramPK");
                    table.ForeignKey(
                        name: "FK_tconsumption_muser",
                        column: x => x.userId,
                        principalTable: "muser",
                        principalColumn: "userPk",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_mconsumption_createdBy",
                table: "mconsumption",
                column: "createdBy");

            migrationBuilder.CreateIndex(
                name: "IX_mconsumption_name_calories",
                table: "mconsumption",
                columns: new[] { "name", "calories" });

            migrationBuilder.CreateIndex(
                name: "IX_mparam_paramType",
                table: "mparam",
                column: "paramType");

            migrationBuilder.CreateIndex(
                name: "IX_mparamnotif_key",
                table: "mparamnotif",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mparamnotif_type",
                table: "mparamnotif",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "IX_mprofile_baseActLevel",
                table: "mprofile",
                column: "baseActLevel");

            migrationBuilder.CreateIndex(
                name: "IX_mprofile_gender",
                table: "mprofile",
                column: "gender");

            migrationBuilder.CreateIndex(
                name: "IX_mprofile_metricParam",
                table: "mprofile",
                column: "metricParam");

            migrationBuilder.CreateIndex(
                name: "IX_mprofile_userId",
                table: "mprofile",
                column: "userId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_muser_email",
                table: "muser",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tconsumption_consumptionId",
                table: "tconsumption",
                column: "consumptionId");

            migrationBuilder.CreateIndex(
                name: "IX_tconsumption_mealType",
                table: "tconsumption",
                column: "mealType");

            migrationBuilder.CreateIndex(
                name: "IX_tconsumption_userId_entryTimestamp",
                table: "tconsumption",
                columns: new[] { "userId", "entryTimestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_tdailyrecord_createdVia",
                table: "tdailyrecord",
                column: "createdVia");

            migrationBuilder.CreateIndex(
                name: "IX_tdailyrecord_deficitCategory",
                table: "tdailyrecord",
                column: "deficitCategory");

            migrationBuilder.CreateIndex(
                name: "IX_tdailyrecord_userId_recordDate",
                table: "tdailyrecord",
                columns: new[] { "userId", "recordDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tfreeze_userId",
                table: "tfreeze",
                column: "userId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tnotification_userId",
                table: "tnotification",
                column: "userId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tstreak_userId",
                table: "tstreak",
                column: "userId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tweightlog_userId_checkpointDate",
                table: "tweightlog",
                columns: new[] { "userId", "checkpointDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mparamnotif");

            migrationBuilder.DropTable(
                name: "mprofile");

            migrationBuilder.DropTable(
                name: "tconsumption");

            migrationBuilder.DropTable(
                name: "tdailyrecord");

            migrationBuilder.DropTable(
                name: "tfreeze");

            migrationBuilder.DropTable(
                name: "tnotification");

            migrationBuilder.DropTable(
                name: "tstreak");

            migrationBuilder.DropTable(
                name: "tweightlog");

            migrationBuilder.DropTable(
                name: "mconsumption");

            migrationBuilder.DropTable(
                name: "mparam");

            migrationBuilder.DropTable(
                name: "muser");
        }
    }
}
