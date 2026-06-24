namespace LRBalanceLock.App.Models;

public sealed record BalanceStatus(
    string State,
    float? Left,
    float? Right,
    DateTimeOffset? LastCorrection,
    string? DeviceName,
    string? Message = null);
