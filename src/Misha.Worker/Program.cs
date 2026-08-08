using Microsoft.Extensions.Hosting;
using Misha.Worker;

var builder = new HostApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
