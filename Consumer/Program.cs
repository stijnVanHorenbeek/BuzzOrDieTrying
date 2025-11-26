using Consumer;
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
    x.AddConsumer<RequestMessageConsumer>();
    
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("rabbitmq", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });
        
        cfg.ReceiveEndpoint("request_queue", e =>
        {
            e.UseMessageRetry(r =>
            {
                r.Exponential(
                    retryLimit: 5,
                    intervalDelta: TimeSpan.FromSeconds(1),
                    maxInterval: TimeSpan.FromSeconds(10),
                    minInterval: TimeSpan.FromSeconds(1)
                );
            });

            e.ConfigureConsumer<RequestMessageConsumer>(context);
        });
    });
});
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
