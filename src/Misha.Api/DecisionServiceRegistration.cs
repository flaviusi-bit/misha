using Misha.Application.Decisions;
using Misha.Application.ManualReviews;
using Misha.Infrastructure.ManualReviews;
using Misha.Infrastructure.Persistence;

namespace Misha.Api;

public static class DecisionServiceRegistration
{
    public static void AddDecisionEngine(IServiceCollection services)
    {
        services.AddSingleton<IDecisionEngine, DefaultDecisionEngine>();
        services.AddScoped<IDecisionAuditRepository, EfDecisionAuditRepository>();
        services.AddScoped<IManualReviewRepository, EfManualReviewRepository>();
        services.AddScoped<DecisionService>();
        services.AddScoped<ManualReviewService>();
        services.AddHostedService<DecisionAuditSchemaInitializer>();
    }
}
