using Consumer;
using MassTransit;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(new
        LoggerConfiguration()
    .WriteTo.Console(
        theme: AnsiConsoleTheme.Code,
        outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
        applyThemeToRedirectedOutput: true)
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
            var enableRetry = builder.Configuration.GetValue("Features:EnableRetry", true);

            if (enableRetry)
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
            }

            e.UseInMemoryOutbox(context);
            e.ConfigureConsumer<RequestMessageConsumer>(context);
        });
    });
});

var host = builder.Build();
host.Run();
