using Atlas.ServiceDefaults;
using Atlas.ServiceDefaults.Auth;
using Atlas.ServiceDefaults.Messaging;
using Atlas.Verification.Worker.Clients;
using Atlas.Verification.Worker.Messaging;
using Atlas.Verification.Worker.Services;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

builder.AddAtlasServiceDefaults("verification-worker");
builder.Services.AddAtlasTokenIssuer(builder.Configuration);

var endpoints = builder.Configuration.GetSection("Endpoints");

builder.Services.AddTransient<ServiceTokenHandler>();
builder.Services
    .AddHttpClient<OnboardingClient>(c => c.BaseAddress = new Uri(endpoints["Onboarding"]!))
    .AddHttpMessageHandler<ServiceTokenHandler>()
    .AddStandardResilienceHandler();

// Standard resilience = per-attempt timeout, 3 retries with exponential backoff + jitter, circuit breaker.
// Short blips are absorbed here; longer outages are handled by MassTransit's message retry.
builder.Services
    .AddHttpClient<IdNowClient>(c => c.BaseAddress = new Uri(endpoints["IdNow"]!))
    .AddStandardResilienceHandler();
builder.Services
    .AddHttpClient<WorldCheckClient>(c => c.BaseAddress = new Uri(endpoints["WorldCheck"]!))
    .AddStandardResilienceHandler();

builder.Services.AddScoped<IVerificationService, VerificationService>();
builder.Services.AddAtlasMessaging(builder.Configuration, bus => bus.AddConsumer<ApplicationSubmittedConsumer>());

var app = builder.Build();
app.UseAtlasServiceDefaults();
app.Run();
