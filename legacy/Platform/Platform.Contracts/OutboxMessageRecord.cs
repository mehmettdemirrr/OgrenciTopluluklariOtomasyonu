namespace Platform.Contracts;

public sealed record OutboxMessageRecord(
    Guid Id,
    string Type,
    string Payload,
    DateTimeOffset OccurredOnUtc,
    DateTimeOffset? ProcessedOnUtc,
    string? Error);