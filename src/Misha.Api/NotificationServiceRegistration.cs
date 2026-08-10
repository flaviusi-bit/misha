using Misha.Application.Notifications;
using Misha.Infrastructure.Notifications;

namespace Misha.Api;

public static class NotificationServiceRegistration
{
    public static void AddNotificationServices(IServiceCollection services)
    {
        services.AddScoped<INotificationRepository, EfNotificationRepository>();
        services.AddScoped<NotificationService>();
    }
}
