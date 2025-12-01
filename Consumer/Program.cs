using Consumer;
using HealthChecks.UI.Client;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(new LoggerConfiguration()
        .Enrich.FromLogContext()
        .WriteTo.Console(theme: AnsiConsoleTheme.Code,
            outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
            applyThemeToRedirectedOutput: true)
        .MinimumLevel.Debug()
        .ReadFrom.Configuration(builder.Configuration)
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
            var useErrorFilter = builder.Configuration.GetValue("Features:UseErrorFilter", false);
            if (useErrorFilter)
            {
                e.ConfigureError(x =>
                        {
                            x.UseFilter(new ShortFaultHeaderFilter());
                        });
            }

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

            // e.UseInMemoryOutbox(context);
            e.ConfigureConsumer<RequestMessageConsumer>(context);
        });
    });
});

var host = builder.Build();
host.UseRouting();
host.MapHealthChecks("/health/ready", new HealthCheckOptions()
{
    Predicate = (check) => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
    ResultStatusCodes = {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
    },
});

host.MapHealthChecks("/health/live", new HealthCheckOptions());
host.Run();
