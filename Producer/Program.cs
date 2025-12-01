using Contracts;
using Producer;
using MassTransit;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(new LoggerConfiguration()
        .Enrich.FromLogContext()
        .WriteTo.Console(
            theme: AnsiConsoleTheme.Code,
            outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
            applyThemeToRedirectedOutput: true)
        .MinimumLevel.Debug()
        .ReadFrom.Configuration(builder.Configuration)
        .CreateLogger());

builder.Services.AddHealthChecks();
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<SimpleBatchConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("rabbitmq", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        cfg.ReceiveEndpoint("simple_message_queue", e =>
        {
            var useErrorFilter = builder.Configuration.GetValue("Features:UseErrorFilter", false);
            if (useErrorFilter)
            {
                e.ConfigureError(x =>
                        {
                            x.UseFilter(new ShortFaultHeaderFilter());
                        });
            }
            e.UseInMemoryOutbox(context);
            e.PrefetchCount = 500;
            e.Batch<SimpleMessage>(b =>
            {
                b.MessageLimit = 250;
                b.TimeLimit = TimeSpan.FromSeconds(2);
                b.Consumer<SimpleBatchConsumer, SimpleMessage>(context);
            });
        });
    });

    x.AddRequestClient<RequestMessage>();
});
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
