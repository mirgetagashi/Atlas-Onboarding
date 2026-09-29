using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;

namespace Atlas.ServiceDefaults;

public static class ServiceDefaultsExtensions
{
    /// <summary>Logging to console + Seq, ProblemDetails errors, health checks, JSON options, clock, Swagger.</summary>
    public static WebApplicationBuilder AddAtlasServiceDefaults(this WebApplicationBuilder builder, string serviceName)
    {
        var seqUrl = builder.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341";

        builder.Host.UseSerilog((_, logger) => logger
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Service", serviceName)
            .WriteTo.Console()
            .WriteTo.Seq(seqUrl));

        builder.Services.AddApiErrorLog(builder.Configuration, serviceName);
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<BadRequestExceptionHandler>();
        builder.Services.AddHealthChecks();
        builder.Services.AddSingleton(TimeProvider.System);

        // Controllers. Enums travel as UPPER_SNAKE_CASE strings ("NATIONAL_ID", "APPROVED"), matching the ticket's API style.
        builder.Services
            .AddControllers()
            .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper)));

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(o =>
        {
            o.SwaggerDoc("v1", new OpenApiInfo { Title = serviceName, Version = "v1" });
            o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Staff token (Backoffice: POST /dev/token).",
            });
            o.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
                    Array.Empty<string>()
                },
            });
        });

        return builder;
    }

    public static WebApplication UseAtlasServiceDefaults(this WebApplication app)
    {
        app.UseApiErrorLogging();
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            app.MapGet("/", () => Results.Redirect("/swagger"));
        }

        app.MapHealthChecks("/health");
        return app;
    }

    /// <summary>Call after UseAuthentication/UseAuthorization so the controllers are protected.</summary>
    public static WebApplication MapAtlasControllers(this WebApplication app)
    {
        app.MapControllers();
        return app;
    }
}
