namespace CareerAdvisor.Core.Careers;

/// <summary>
/// Defines the permanent database identifiers for the supported career catalogue.
/// These identifiers must never change because recommendation records reference them.
/// </summary>
public static class CareerCatalogIdentity
{
    public static IReadOnlyDictionary<string, Guid> IdsByCode { get; } =
        new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase)
        {
            ["SD-001"] = Guid.Parse(
                "10000000-0000-0000-0000-000000000001"),
            ["DA-002"] = Guid.Parse(
                "10000000-0000-0000-0000-000000000002"),
            ["CS-003"] = Guid.Parse(
                "10000000-0000-0000-0000-000000000003"),
            ["CE-004"] = Guid.Parse(
                "10000000-0000-0000-0000-000000000004"),
            ["NA-005"] = Guid.Parse(
                "10000000-0000-0000-0000-000000000005"),
            ["DBA-006"] = Guid.Parse(
                "10000000-0000-0000-0000-000000000006"),
            ["UX-007"] = Guid.Parse(
                "10000000-0000-0000-0000-000000000007"),
            ["AI-008"] = Guid.Parse(
                "10000000-0000-0000-0000-000000000008")
        };

    public static Guid GetId(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var normalizedCode = code.Trim();

        if (!IdsByCode.TryGetValue(normalizedCode, out var id))
        {
            throw new KeyNotFoundException(
                $"Career code '{normalizedCode}' is not recognized.");
        }

        return id;
    }

    public static bool TryGetId(string? code, out Guid id)
    {
        id = Guid.Empty;

        return !string.IsNullOrWhiteSpace(code)
            && IdsByCode.TryGetValue(code.Trim(), out id);
    }
}