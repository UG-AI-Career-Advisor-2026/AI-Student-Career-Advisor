using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareerAdvisor.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CareerProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    RequiredSkills = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudentProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Programme = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    AcademicLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    Interests = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LearningRoadmaps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StudentProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CareerProfileId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningRoadmaps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearningRoadmaps_CareerProfiles_CareerProfileId",
                        column: x => x.CareerProfileId,
                        principalTable: "CareerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LearningRoadmaps_StudentProfiles_StudentProfileId",
                        column: x => x.StudentProfileId,
                        principalTable: "StudentProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecommendationSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StudentProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecommendationSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecommendationSessions_StudentProfiles_StudentProfileId",
                        column: x => x.StudentProfileId,
                        principalTable: "StudentProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentSkills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SkillName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Proficiency = table.Column<int>(type: "INTEGER", nullable: false),
                    StudentProfileId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentSkills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentSkills_StudentProfiles_StudentProfileId",
                        column: x => x.StudentProfileId,
                        principalTable: "StudentProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoadmapSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    ResourceLink = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    LearningRoadmapId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadmapSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoadmapSteps_LearningRoadmaps_LearningRoadmapId",
                        column: x => x.LearningRoadmapId,
                        principalTable: "LearningRoadmaps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CareerRecommendations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CareerProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MatchScore = table.Column<double>(type: "REAL", nullable: false),
                    Reasoning = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    RecommendationSessionId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerRecommendations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CareerRecommendations_CareerProfiles_CareerProfileId",
                        column: x => x.CareerProfileId,
                        principalTable: "CareerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CareerRecommendations_RecommendationSessions_RecommendationSessionId",
                        column: x => x.RecommendationSessionId,
                        principalTable: "RecommendationSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CareerRecommendations_CareerProfileId",
                table: "CareerRecommendations",
                column: "CareerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_CareerRecommendations_RecommendationSessionId",
                table: "CareerRecommendations",
                column: "RecommendationSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningRoadmaps_CareerProfileId",
                table: "LearningRoadmaps",
                column: "CareerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningRoadmaps_StudentProfileId",
                table: "LearningRoadmaps",
                column: "StudentProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationSessions_StudentProfileId",
                table: "RecommendationSessions",
                column: "StudentProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadmapSteps_LearningRoadmapId",
                table: "RoadmapSteps",
                column: "LearningRoadmapId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentSkills_StudentProfileId",
                table: "StudentSkills",
                column: "StudentProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CareerRecommendations");

            migrationBuilder.DropTable(
                name: "RoadmapSteps");

            migrationBuilder.DropTable(
                name: "StudentSkills");

            migrationBuilder.DropTable(
                name: "RecommendationSessions");

            migrationBuilder.DropTable(
                name: "LearningRoadmaps");

            migrationBuilder.DropTable(
                name: "CareerProfiles");

            migrationBuilder.DropTable(
                name: "StudentProfiles");
        }
    }
}
