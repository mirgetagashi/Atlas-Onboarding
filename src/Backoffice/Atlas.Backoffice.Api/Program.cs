using Atlas.Backoffice.Api.Infrastructure.Messaging;
using Atlas.Backoffice.Api.Infrastructure.Onboarding;
using Atlas.Backoffice.Api.Infrastructure.Persistence;
using Atlas.Backoffice.Api.Infrastructure.Security;
using Atlas.Backoffice.Api.UseCases.Abstractions;
using Atlas.Backoffice.Api.UseCases.Services;
using Atlas.ServiceDefaults;
using Atlas.ServiceDefaults.Auth;
using Atlas.ServiceDefaults.Messaging;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Cross-cutting: logging, errors, controllers, auth, messaging.
builder.AddAtlasServiceDefaults("backoffice-api");
builder.Services.AddAtlasJwtAuthentication(builder.Configuration);
builder.Services.AddAtlasMessaging(builder.Configuration, bus => bus.AddConsumer<ApplicationReferredForReviewConsumer>());

// UseCases (logic).
builder.Services.AddScoped<IReviewCaseService, ReviewCaseService>();
builder.Services.AddScoped<IBranchActivationService, BranchActivationService>();
builder.Services.AddScoped<IDevTokenService, DevTokenService>();

// Infrastructure: database, Onboarding gateway (forwarding the staff member's token), current user.
builder.Services.AddDbContext<BackofficeDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Backoffice"), sql => sql.EnableRetryOnFailure()));
builder.Services.AddHostedService<BackofficeDatabaseInitializer>();
builder.Services.AddScoped<IReviewCaseRepository, ReviewCaseRepository>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddTransient<ForwardUserTokenHandler>();
builder.Services
    .AddHttpClient<IOnboardingGateway, OnboardingHttpGateway>(c => c.BaseAddress = new Uri(builder.Configuration["Endpoints:Onboarding"]!))
    .AddHttpMessageHandler<ForwardUserTokenHandler>()
    .AddStandardResilienceHandler();

var app = builder.Build();

app.UseAtlasServiceDefaults();
app.UseAuthentication();
app.UseAuthorization();
app.MapAtlasControllers();

app.Run();
