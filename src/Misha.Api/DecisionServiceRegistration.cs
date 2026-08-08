using Misha.Application.Decisions;
using Misha.Infrastructure.Persistence;

namespace Misha.Api;

public static class DecisionServiceRegistration
{
    public static void AddDecisionEngine(IServiceCollection services)
    {
        services.AddSingleton<IDecisionEngine, DefaultDecisionEngine>();
        services.AddScoped<IDecisionAuditRepository, EfDecisionAuditRepository>();
        services.AddScoped<DecisionService>();
        services.AddHostedService<DecisionAuditSchemaInitializer>();
    }
}
