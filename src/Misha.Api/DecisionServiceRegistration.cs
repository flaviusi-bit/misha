using Misha.Application.Decisions;
using Misha.Application.Watchlists;
using Misha.Infrastructure.Persistence;
using Misha.Infrastructure.Watchlists;

namespace Misha.Api;

public static class DecisionServiceRegistration
{
    public static void AddDecisionEngine(IServiceCollection services)
    {
        services.AddSingleton<IDecisionEngine, DefaultDecisionEngine>();
        services.AddScoped<IDecisionAuditRepository, EfDecisionAuditRepository>();
        services.AddScoped<DecisionService>();
        services.AddHostedService<DecisionAuditSchemaInitializer>();

        // Registered after the legacy unavailable provider so this becomes the active gateway.
        services.AddHttpClient("watchlist", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddScoped<IWatchlistProvider, HttpWatchlistProvider>();
    }
}
