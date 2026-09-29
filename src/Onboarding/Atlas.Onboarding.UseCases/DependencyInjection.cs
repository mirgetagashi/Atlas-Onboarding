using Atlas.Onboarding.UseCases.Services;
using Atlas.Onboarding.UseCases.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Onboarding.UseCases;

public static class DependencyInjection
{
    public static IServiceCollection AddOnboardingUseCases(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OnboardingOptions>(configuration.GetSection(OnboardingOptions.SectionName));

        services.AddScoped<IMarketService, MarketService>();
        services.AddScoped<IApplicantService, ApplicantService>();
        services.AddScoped<IStaffApplicationService, StaffApplicationService>();
        services.AddScoped<IVerificationResultService, VerificationResultService>();

        return services;
    }
}
