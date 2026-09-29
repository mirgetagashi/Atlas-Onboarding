using Atlas.Markets;
using Atlas.Onboarding.Api.ErrorHandling;
using Atlas.Onboarding.Api.Messaging;
using Atlas.Onboarding.Api.Security;
using Atlas.Onboarding.Infrastructure;
using Atlas.Onboarding.UseCases;
using Atlas.Onboarding.UseCases.Abstractions;
using Atlas.ServiceDefaults;
using Atlas.ServiceDefaults.Auth;
using Atlas.ServiceDefaults.Messaging;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

// Cross-cutting: logging, errors, controllers, auth, messaging.
builder.AddAtlasServiceDefaults("onboarding-api");
builder.AddAtlasMarkets();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddAtlasJwtAuthentication(builder.Configuration);
builder.Services.AddAtlasMessaging(builder.Configuration, bus => bus.AddConsumer<VerificationCompletedConsumer>());

// Application layers and the current user taken from the request.
builder.Services.AddOnboardingUseCases(builder.Configuration);
builder.Services.AddOnboardingInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentStaff, HttpCurrentStaff>();

var app = builder.Build();

app.UseAtlasServiceDefaults();
app.UseAuthentication();
app.UseAuthorization();
app.MapAtlasControllers();

app.Run();

/// <summary>Exposed so integration tests can host the API with WebApplicationFactory.</summary>
public partial class Program
{
}
