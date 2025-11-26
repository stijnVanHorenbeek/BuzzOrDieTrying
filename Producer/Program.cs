using Contracts;
using Producer;
using MassTransit;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(new 
    LoggerConfiguration()
    .WriteTo.Console()
    .MinimumLevel.Debug()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger());

builder.Services.AddHealthChecks();
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("rabbitmq", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

    });

    x.AddRequestClient<RequestMessage>();
});
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
