namespace Atlas.Providers.Mock.Models;

public sealed record ChaosSnapshot(double FailureRate, int LatencyMs);
