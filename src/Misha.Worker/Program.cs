using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Misha.Application.Applications;
using Misha.Application.Messaging;
using Misha.Application.Notifications;
using Misha.Application.Retention;
using Misha.Infrastructure.Messaging;
using Misha.Infrastructure.Notifications;
using Misha.Infrastructure.Observability;
using Misha.Infrastructure.Persistence;
using Misha.Infrastructure.Retention;
using Misha.Worker;

var builder = new HostApplicationBuilder(args);

builder.Services.AddMishaOpenTelemetry(builder.Configuration, "misha-worker", includeAspNetCoreInstrumentation: false);
builder.Services.Configure<RetentionOptions>(builder.Configuration.GetSection(RetentionOptions.SectionName));
builder.Services.Configure<NotificationDeliveryOptions>(builder.Configuration.GetSection(NotificationDeliveryOptions.SectionName));

var connectionString = builder.Configuration.GetConnectionString("Misha");
if (string.IsNullOrWhiteSpace(connectionString))
{
    var dbHost = builder.Configuration["DB_HOST"];
    var dbPort = builder.Configuration["DB_PORT"] ?? "5432";
    var dbName = builder.Configuration["DB_NAME"];
    var dbUser = builder.Configuration["DB_USER"];
    var dbPassword = builder.Configuration["DB_PASSWORD"];

    if (string.IsNullOrWhiteSpace(dbHost) || string.IsNullOrWhiteSpace(dbName) || string.IsNullOrWhiteSpace(dbUser) || string.IsNullOrWhiteSpace(dbPassword))
    {
        throw new InvalidOperationException("Database configuration is missing. Expected ConnectionStrings:Misha or DB_HOST, DB_PORT, DB_NAME, DB_USER and DB_PASSWORD settings.");
    }

    connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword};SSL Mode=Require";
}

var queueUrl = builder.Configuration["Outbox:QueueUrl"];
if (string.IsNullOrWhiteSpace(queueUrl))
{
    throw new InvalidOperationException("Outbox queue configuration is missing. Expected Outbox:QueueUrl.");
}

builder.Services.AddDbContext<MishaDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddSingleton<Amazon.SQS.IAmazonSQS>(_ => new Amazon.SQS.AmazonSQSClient());

builder.Services.AddScoped<IApplicationRepository, EfApplicationRepository>();
builder.Services.AddScoped<INotificationRepository, EfNotificationRepository>();
builder.Services.AddScoped<IEventIdempotencyStore, EfEventIdempotencyStore>();
builder.Services.AddScoped<IEventHandler, ApplicationLifecycleChangedHandler>();
builder.Services.AddScoped<EventDispatcher>();
builder.Services.AddScoped<ISqsMessageConsumer>(sp => new SqsMessageConsumer(
    sp.GetRequiredService<Amazon.SQS.IAmazonSQS>(),
    queueUrl,
    sp.GetRequiredService<ILogger<SqsMessageConsumer>>()));

builder.Services.AddScoped<IOutboxDispatcher>(sp => new SqsOutboxDispatcher(
    sp.GetRequiredService<MishaDbContext>(),
    sp.GetRequiredService<Amazon.SQS.IAmazonSQS>(),
    queueUrl,
    sp.GetRequiredService<ILogger<SqsOutboxDispatcher>>()));

builder.Services.AddHttpClient<INotificationDelivery, HttpNotificationDelivery>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
    client.MaxResponseContentBufferSize = 64 * 1024;
});
builder.Services.AddHostedService<NotificationDeliveryWorker>();
builder.Services.AddScoped<IRetentionPurgeService, RetentionPurgeService>();
builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<RetentionWorker>();

var host = builder.Build();
host.Run();