using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.ServiceDefaults.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Host { get; set; } = "localhost";

    public string VirtualHost { get; set; } = "/";

    public string Username { get; set; } = "guest";

    public string Password { get; set; } = "guest";
}

public static class MessagingExtensions
{
    /// <summary>
    /// RabbitMQ via MassTransit. Failed messages are retried with growing delays
    /// (covers a provider outage of a few minutes); after that they land in an _error queue
    /// instead of being lost, and the application simply stays "Submitted".
    /// </summary>
    public static IServiceCollection AddAtlasMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? registerConsumers = null)
    {
        var rabbit = configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>() ?? new RabbitMqOptions();

        services.AddMassTransit(bus =>
        {
            bus.SetKebabCaseEndpointNameFormatter();
            registerConsumers?.Invoke(bus);

            bus.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbit.Host, rabbit.VirtualHost, host =>
                {
                    host.Username(rabbit.Username);
                    host.Password(rabbit.Password);
                });

                // 10 retries, from 1 second up to 2 minutes apart.
                cfg.UseMessageRetry(retry => retry.Exponential(
                    10,
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromMinutes(2),
                    TimeSpan.FromSeconds(3)));

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
