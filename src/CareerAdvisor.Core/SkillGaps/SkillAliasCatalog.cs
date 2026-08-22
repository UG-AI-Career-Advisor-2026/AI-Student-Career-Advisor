using System.Collections.Frozen;
using System.Collections.ObjectModel;

namespace CareerAdvisor.Core.SkillGaps;

/// <summary>
/// Defines the narrow, explicitly approved aliases for career-catalogue skills.
/// Canonical requirement phrases are accepted independently of this catalogue.
/// </summary>
public static class SkillAliasCatalog
{
    /// <summary>
    /// Gets immutable approved aliases keyed by the exact career-catalogue
    /// requirement used for display.
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>>
        AliasesByRequiredSkill { get; }

    internal static IReadOnlyDictionary<string, IReadOnlyList<string>>
        AliasesByNormalizedRequiredSkill { get; }

    static SkillAliasCatalog()
    {
        var definitions = GetDefinitions();

        var duplicateNormalizedKey = definitions
            .GroupBy(
                definition => SkillNameNormalizer.Normalize(
                    definition.RequiredSkillName),
                StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateNormalizedKey is not null)
        {
            throw new InvalidOperationException(
                "Skill alias definitions contain duplicate normalized " +
                $"required-skill key '{duplicateNormalizedKey.Key}'.");
        }

        var immutableDefinitions = definitions
            .Select(definition => new AliasDefinition(
                definition.RequiredSkillName,
                new ReadOnlyCollection<string>(
                    definition.Aliases.ToArray())))
            .ToList();

        AliasesByRequiredSkill = immutableDefinitions
            .ToFrozenDictionary(
                definition => definition.RequiredSkillName,
                definition => definition.Aliases,
                StringComparer.Ordinal);

        AliasesByNormalizedRequiredSkill = immutableDefinitions
            .ToFrozenDictionary(
                definition => SkillNameNormalizer.Normalize(
                    definition.RequiredSkillName),
                definition => definition.Aliases,
                StringComparer.Ordinal);
    }

    private static IReadOnlyList<AliasDefinition> GetDefinitions()
    {
        return
        [
            new("C# or Java Programming",
                ["C#", "C# Programming", "Java", "Java Programming"]),
            new("Python Scripting", ["Python"]),
            new("Git Version Control", ["Git"]),
            new("RESTful API Design", ["RESTful API", "REST API"]),
            new("SQL and Database Integration", ["SQL"]),
            new("SQL Querying", ["SQL"]),
            new("Python or R Programming",
                ["Python", "Python Programming", "R", "R Programming"]),
            new("Data Visualization (Tableau/Power BI)",
                ["Data Visualization", "Tableau", "Power BI"]),
            new("Excel/Spreadsheets", ["Excel", "Spreadsheets"]),
            new("Scripting (Python/Bash)",
                ["Python", "Bash", "Python Scripting", "Bash Scripting"]),
            new("AWS/Azure/GCP Platforms",
                [
                    "AWS",
                    "Amazon Web Services",
                    "Azure",
                    "Microsoft Azure",
                    "GCP",
                    "Google Cloud Platform"
                ]),
            new("Infrastructure as Code (Terraform)",
                ["Infrastructure as Code", "IaC", "Terraform"]),
            new("CI/CD Integration", ["CI/CD"]),
            new("TCP/IP Protocol Suite", ["TCP/IP"]),
            new("Packet Analysis (Wireshark)",
                ["Packet Analysis", "Wireshark"]),
            new("SQL Server / PostgreSQL / MySQL",
                ["SQL Server", "PostgreSQL", "Postgres", "MySQL"]),
            new("Scripting (Bash/Python)",
                ["Bash", "Python", "Bash Scripting", "Python Scripting"]),
            new("Figma / Adobe XD", ["Figma", "Adobe XD"]),
            new("TensorFlow or PyTorch", ["TensorFlow", "PyTorch"]),
            new("Mathematics (Linear Algebra/Calculus)",
                ["Linear Algebra", "Calculus"])
        ];
    }

    private sealed record AliasDefinition(
        string RequiredSkillName,
        IReadOnlyList<string> Aliases);
}
