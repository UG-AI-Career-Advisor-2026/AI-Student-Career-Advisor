using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareerAdvisor.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class StabilizeRecommendationPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM "CareerRecommendations"
                WHERE "RecommendationSessionId" IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "RecommendationSessionId",
                table: "CareerRecommendations",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CareerProfiles_Code",
                table: "CareerProfiles",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CareerProfiles_Code",
                table: "CareerProfiles");

            migrationBuilder.AlterColumn<Guid>(
                name: "RecommendationSessionId",
                table: "CareerRecommendations",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");
        }
    }
}
