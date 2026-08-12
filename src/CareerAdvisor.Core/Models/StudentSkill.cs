using CareerAdvisor.Core.Enums;

namespace CareerAdvisor.Core.Models;

public class StudentSkill
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SkillName { get; set; } = string.Empty;
    public SkillProficiency Proficiency { get; set; }
    public Guid? StudentProfileId { get; set; } // For EF Core relationship later
}