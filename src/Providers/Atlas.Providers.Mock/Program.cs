using Atlas.Providers.Mock.Filters;
using Atlas.Providers.Mock.Services;
using Atlas.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddAtlasServiceDefaults("providers-mock");
builder.Services.AddSingleton<MockProviderRules>();
builder.Services.AddSingleton(new ChaosSettings(
    builder.Configuration.GetValue("Chaos:FailureRate", 0.0),
    builder.Configuration.GetValue("Chaos:LatencyMs", 300)));
builder.Services.AddScoped<ChaosFilter>();

var app = builder.Build();

app.UseAtlasServiceDefaults();
app.MapAtlasControllers();

app.Run();
