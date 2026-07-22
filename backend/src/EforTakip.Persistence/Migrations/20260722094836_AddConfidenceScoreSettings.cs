using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EforTakip.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConfidenceScoreSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfidenceScoreSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WeightDescriptionLength = table.Column<int>(type: "integer", nullable: false),
                    WeightSpecificity = table.Column<int>(type: "integer", nullable: false),
                    WeightGenericPenalty = table.Column<int>(type: "integer", nullable: false),
                    WeightDuplicateDetection = table.Column<int>(type: "integer", nullable: false),
                    WeightRoundHoursSingle = table.Column<int>(type: "integer", nullable: false),
                    WeightDurationDescriptionRatio = table.Column<int>(type: "integer", nullable: false),
                    WeightDailyRoundTotal = table.Column<int>(type: "integer", nullable: false),
                    WeightDailyTotalReasonableness = table.Column<int>(type: "integer", nullable: false),
                    WeightBaselineDeviation = table.Column<int>(type: "integer", nullable: false),
                    WeightWeekendHoliday = table.Column<int>(type: "integer", nullable: false),
                    ThresholdVeryLow = table.Column<int>(type: "integer", nullable: false),
                    ThresholdLow = table.Column<int>(type: "integer", nullable: false),
                    ThresholdMedium = table.Column<int>(type: "integer", nullable: false),
                    ThresholdHigh = table.Column<int>(type: "integer", nullable: false),
                    BaselineLookbackDays = table.Column<int>(type: "integer", nullable: false),
                    DuplicateLookbackDays = table.Column<int>(type: "integer", nullable: false),
                    DuplicateSimilarityThreshold = table.Column<decimal>(type: "numeric(4,3)", precision: 4, scale: 3, nullable: false),
                    ShortDescriptionCharThreshold = table.Column<int>(type: "integer", nullable: false),
                    LongDescriptionCharThreshold = table.Column<int>(type: "integer", nullable: false),
                    LongDurationHoursThreshold = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    ShortDurationHoursThreshold = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    DailyTotalSuspiciousHours = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    GenericPhrasesCsv = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfidenceScoreSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfidenceScoreSettings");
        }
    }
}
