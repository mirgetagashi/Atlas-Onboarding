using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Markets;

public static class MarketsRegistration
{
    /// <summary>
    /// Loads markets.json (single source of truth for Annex B) and registers <see cref="IMarketCatalog"/>.
    /// The file is read from the output folder, so it works no matter which project is started.
    /// </summary>
    public static WebApplicationBuilder AddAtlasMarkets(this WebApplicationBuilder builder)
    {
        builder.Configuration.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "markets.json"), optional: false);
        builder.Services.AddSingleton<IMarketCatalog>(MarketCatalog.FromConfiguration(builder.Configuration));
        return builder;
    }
}
