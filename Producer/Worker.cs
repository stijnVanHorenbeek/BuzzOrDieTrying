using Contracts;
using MassTransit;

namespace Producer;

public class Worker : BackgroundService
{
    private readonly IBus _bus;
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private int _counter = 0;
    private const int MaxMessages = 20_000;

    public Worker(IBus bus, ILogger<Worker> logger, IConfiguration configuration)
    {
        _bus = bus;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested && _counter < MaxMessages)
        {
            _logger.LogInformation("Publishing batch of 100 messages starting at counter: {Counter}", _counter + 1);

            var handleErrors = _configuration.GetValue("Features:HandleErrors", true);
            var delayInMs = _configuration.GetValue("Worker:DelayInMs", 500);

            var messages = new List<SimpleMessage>();
            var batchSize = Math.Min(100, MaxMessages - _counter);
            for (int i = 0; i < batchSize; i++)
            {
                _counter++;
                messages.Add(new SimpleMessage { Counter = _counter });
            }

            if (handleErrors)
            {
                try
                {
                    await _bus.PublishBatch(messages, stoppingToken);
                    _logger.LogInformation("Successfully published {Count} messages ending at counter: {Counter}", batchSize, _counter);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while publishing batch. Exception Type: {ExceptionType}", ex.GetType().Name);
                }
            }
            else
            {
                _logger.LogWarning("Error handling DISABLED - exceptions will crash the producer");
                await _bus.PublishBatch(messages, stoppingToken);
                _logger.LogInformation("Successfully published {Count} messages ending at counter: {Counter}", batchSize, _counter);
            }

            await Task.Delay(TimeSpan.FromMilliseconds(delayInMs), stoppingToken);
        }

        if (_counter >= MaxMessages)
        {
            _logger.LogInformation("Reached maximum of {MaxMessages} messages. Worker stopping.", MaxMessages);
        }
    }
}
