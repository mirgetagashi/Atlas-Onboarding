using System.Text.Json;
using System.Text.Json.Serialization;
using Atlas.Contracts;

namespace Atlas.Onboarding.Infrastructure.Outbox;

/// <summary>An integration event waiting to be published to RabbitMQ. Stored in the market's own database.</summary>
public sealed class OutboxMessage
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public Guid Id { get; private set; }

    public string Type { get; private set; } = string.Empty;

    public string Payload { get; private set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public int Attempts { get; private set; }

    public string? LastError { get; private set; }

    public static OutboxMessage From(object integrationEvent, DateTimeOffset occurredAt)
    {
        var type = integrationEvent.GetType();
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = type.Name,
            Payload = JsonSerializer.Serialize(integrationEvent, type, SerializerOptions),
            OccurredAt = occurredAt,
        };
    }

    public object Deserialize()
    {
        if (!IntegrationEventTypes.TryResolve(Type, out var type))
        {
            throw new InvalidOperationException($"Unknown integration event type '{Type}'.");
        }

        return JsonSerializer.Deserialize(Payload, type, SerializerOptions)
            ?? throw new InvalidOperationException($"Outbox message {Id} has an empty payload.");
    }

    public void MarkPublished(DateTimeOffset at) => PublishedAt = at;

    public void MarkFailed(string error)
    {
        Attempts++;
        LastError = error.Length > 2000 ? error[..2000] : error;
    }
}
