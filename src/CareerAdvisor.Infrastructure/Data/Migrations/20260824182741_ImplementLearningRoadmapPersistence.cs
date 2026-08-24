using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareerAdvisor.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ImplementLearningRoadmapPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RoadmapSteps_LearningRoadmapId",
                table: "RoadmapSteps");

            migrationBuilder.DropIndex(
                name: "IX_LearningRoadmaps_StudentProfileId",
                table: "LearningRoadmaps");

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "RoadmapSteps",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "LearningRoadmaps",
                type: "TEXT",
                nullable: true);

            // The application had no roadmap writer before this migration,
            // but the tables already existed. Preserve any valid legacy rows
            // with one documented deterministic UTC migration timestamp.
            migrationBuilder.Sql(
                """
                UPDATE "LearningRoadmaps"
                SET "CreatedAt" = '2026-08-24T00:00:00.0000000Z'
                WHERE "CreatedAt" IS NULL;

                UPDATE "RoadmapSteps"
                SET "CompletedAt" = '2026-08-24T00:00:00.0000000Z'
                WHERE "IsCompleted" = 1;
                """);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "LearningRoadmaps",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "LearningRoadmapId",
                table: "RoadmapSteps",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoadmapSteps_LearningRoadmapId_Order",
                table: "RoadmapSteps",
                columns: new[] { "LearningRoadmapId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearningRoadmaps_StudentProfileId_CareerProfileId",
                table: "LearningRoadmaps",
                columns: new[] { "StudentProfileId", "CareerProfileId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RoadmapSteps_LearningRoadmapId_Order",
                table: "RoadmapSteps");

            migrationBuilder.DropIndex(
                name: "IX_LearningRoadmaps_StudentProfileId_CareerProfileId",
                table: "LearningRoadmaps");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "RoadmapSteps");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "LearningRoadmaps");

            migrationBuilder.AlterColumn<Guid>(
                name: "LearningRoadmapId",
                table: "RoadmapSteps",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.CreateIndex(
                name: "IX_RoadmapSteps_LearningRoadmapId",
                table: "RoadmapSteps",
                column: "LearningRoadmapId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningRoadmaps_StudentProfileId",
                table: "LearningRoadmaps",
                column: "StudentProfileId");
        }
    }
}
