using Atlas.Providers.Mock.Models;

namespace Atlas.Providers.Mock.Services;

/// <summary>Simulated provider trouble: a share of calls fail with 503, and every call is slowed down.</summary>
public sealed class ChaosSettings
{
    private readonly object _lock = new();
    private double _failureRate;
    private int _latencyMs;

    public ChaosSettings(double failureRate, int latencyMs) => Update(new ChaosUpdate(failureRate, latencyMs));

    public ChaosSnapshot Snapshot()
    {
        lock (_lock)
        {
            return new ChaosSnapshot(_failureRate, _latencyMs);
        }
    }

    public ChaosSnapshot Update(ChaosUpdate update)
    {
        lock (_lock)
        {
            if (update.FailureRate is { } rate)
            {
                _failureRate = Math.Clamp(rate, 0.0, 1.0);
            }

            if (update.LatencyMs is { } latency)
            {
                _latencyMs = Math.Clamp(latency, 0, 60_000);
            }

            return new ChaosSnapshot(_failureRate, _latencyMs);
        }
    }
}
