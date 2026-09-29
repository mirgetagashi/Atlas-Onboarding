namespace Atlas.Providers.Mock.Models;

public sealed record ChaosUpdate(double? FailureRate, int? LatencyMs);
