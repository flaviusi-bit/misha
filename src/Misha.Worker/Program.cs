using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Misha.Application.Messaging;
using Misha.Infrastructure.Persistence;
using Misha.Worker;

var builder = new HostApplicationBuilder(args);

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

    connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword}";
}

var queueUrl = builder.Configuration["Outbox:QueueUrl"];
if (string.IsNullOrWhiteSpace(queueUrl))
{
    throw new InvalidOperationException("Outbox queue configuration is missing. Expected Outbox:QueueUrl.");
}

builder.Services.AddDbContext<MishaDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddSingleton<Amazon.SQS.IAmazonSQS>(_ => new Amazon.SQS.AmazonSQSClient());
builder.Services.AddScoped<IOutboxDispatcher>(sp => new SqsOutboxDispatcher(
    sp.GetRequiredService<MishaDbContext>(),
    sp.GetRequiredService<Amazon.SQS.IAmazonSQS>(),
    queueUrl,
    sp.GetRequiredService<ILogger<SqsOutboxDispatcher>>()));
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
