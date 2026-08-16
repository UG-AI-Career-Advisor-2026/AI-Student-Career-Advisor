using CareerAdvisor.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace CareerAdvisor.Infrastructure.Data;

public class CareerAdvisorDbContext(
    DbContextOptions<CareerAdvisorDbContext> options)
    : DbContext(options)
{
    public DbSet<StudentProfile> StudentProfiles =>
        Set<StudentProfile>();

    public DbSet<StudentSkill> StudentSkills =>
        Set<StudentSkill>();

    public DbSet<AssessmentSession> AssessmentSessions =>
        Set<AssessmentSession>();

    public DbSet<AssessmentResponse> AssessmentResponses =>
        Set<AssessmentResponse>();

    public DbSet<CareerProfile> CareerProfiles =>
        Set<CareerProfile>();

    public DbSet<RecommendationSession> RecommendationSessions =>
        Set<RecommendationSession>();

    public DbSet<CareerRecommendation> CareerRecommendations =>
        Set<CareerRecommendation>();

    public DbSet<LearningRoadmap> LearningRoadmaps =>
        Set<LearningRoadmap>();

    public DbSet<RoadmapStep> RoadmapSteps =>
        Set<RoadmapStep>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureStudentProfile(modelBuilder);
        ConfigureStudentSkill(modelBuilder);
        ConfigureAssessmentSession(modelBuilder);
        ConfigureAssessmentResponse(modelBuilder);
        ConfigureCareerProfile(modelBuilder);
        ConfigureRecommendationSession(modelBuilder);
        ConfigureCareerRecommendation(modelBuilder);
        ConfigureLearningRoadmap(modelBuilder);
        ConfigureRoadmapStep(modelBuilder);
    }

    private static void ConfigureStudentProfile(
        ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<StudentProfile>();

        entity.HasKey(profile => profile.Id);

        entity.Property(profile => profile.Name)
            .IsRequired()
            .HasMaxLength(150);

        entity.Property(profile => profile.Programme)
            .IsRequired()
            .HasMaxLength(200);

        entity.HasMany(profile => profile.Skills)
            .WithOne()
            .HasForeignKey(skill => skill.StudentProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureStudentSkill(
        ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<StudentSkill>();

        entity.HasKey(skill => skill.Id);

        entity.Property(skill => skill.SkillName)
            .IsRequired()
            .HasMaxLength(100);
    }

    private static void ConfigureAssessmentSession(
        ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<AssessmentSession>();

        entity.HasKey(session => session.Id);

        entity.Property(session => session.Status)
            .IsRequired()
            .HasMaxLength(20);

        entity.HasOne<StudentProfile>()
            .WithMany()
            .HasForeignKey(session => session.StudentProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(session => session.Responses)
            .WithOne()
            .HasForeignKey(response => response.AssessmentSessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureAssessmentResponse(
        ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<AssessmentResponse>();

        entity.HasKey(response => response.Id);

        entity.HasIndex(response => new
            {
                response.AssessmentSessionId,
                response.QuestionId
            })
            .IsUnique();
    }

    private static void ConfigureCareerProfile(
        ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<CareerProfile>();

        entity.HasKey(career => career.Id);

        entity.Property(career => career.Title)
            .IsRequired()
            .HasMaxLength(150);

        entity.Property(career => career.Description)
            .IsRequired()
            .HasMaxLength(1000);
    }

    private static void ConfigureRecommendationSession(
        ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<RecommendationSession>();

        entity.HasKey(session => session.Id);

        entity.HasOne<StudentProfile>()
            .WithMany()
            .HasForeignKey(session => session.StudentProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(session => session.Recommendations)
            .WithOne()
            .HasForeignKey("RecommendationSessionId")
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureCareerRecommendation(
        ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<CareerRecommendation>();

        entity.HasKey(recommendation => recommendation.Id);

        entity.Property(recommendation => recommendation.Reasoning)
            .IsRequired()
            .HasMaxLength(2000);

        entity.HasOne(recommendation => recommendation.Career)
            .WithMany()
            .HasForeignKey(recommendation =>
                recommendation.CareerProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureLearningRoadmap(
        ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<LearningRoadmap>();

        entity.HasKey(roadmap => roadmap.Id);

        entity.HasOne<StudentProfile>()
            .WithMany()
            .HasForeignKey(roadmap => roadmap.StudentProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne<CareerProfile>()
            .WithMany()
            .HasForeignKey(roadmap => roadmap.CareerProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasMany(roadmap => roadmap.Steps)
            .WithOne()
            .HasForeignKey("LearningRoadmapId")
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureRoadmapStep(
        ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<RoadmapStep>();

        entity.HasKey(step => step.Id);

        entity.Property(step => step.Title)
            .IsRequired()
            .HasMaxLength(200);

        entity.Property(step => step.Description)
            .IsRequired()
            .HasMaxLength(1000);

        entity.Property(step => step.ResourceLink)
            .HasMaxLength(1000);
    }
}