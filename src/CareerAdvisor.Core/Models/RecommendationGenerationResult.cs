namespace CareerAdvisor.Core.Models;

/// <summary>
/// Describes the authoritative saved recommendation session returned by a
/// generation request and whether that request persisted a new session. This
/// record is an immutable outcome wrapper; its <see cref="Session"/> is the
/// mutable, detached domain aggregate used throughout the application and is
/// not a deep immutable snapshot.
/// </summary>
public sealed record RecommendationGenerationResult(
    RecommendationSession Session,
    bool WasNewlyPersisted);
