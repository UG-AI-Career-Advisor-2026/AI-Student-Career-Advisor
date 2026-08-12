using CareerAdvisor.Core.Enums;

namespace CareerAdvisor.Core.Models;

public class StudentProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Programme { get; set; } = string.Empty;
    public AcademicLevel AcademicLevel { get; set; }
    public List<string> Interests { get; set; } = new();
    public List<StudentSkill> Skills { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}