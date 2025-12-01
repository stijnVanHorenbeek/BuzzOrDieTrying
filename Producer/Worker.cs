using Contracts;
using MassTransit;

namespace Producer;

public class Worker : BackgroundService
{
    private readonly IBus _bus;
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private int _counter = 0;

    public Worker(IBus bus, ILogger<Worker> logger, IConfiguration configuration)
    {
        _bus = bus;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var MaxMessages = _configuration.GetValue("Worker:MaxMessages", 20_000);
        var delayInMs = _configuration.GetValue("Worker:DelayInMs", 500);
        var batchSize = _configuration.GetValue("Worker:BatchSize", 1000);

        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested && _counter < MaxMessages)
        {
            _logger.LogInformation("Publishing batch of 100 messages starting at counter: {Counter}", _counter + 1);

            var messages = new List<SimpleMessage>();
            batchSize = Math.Min(batchSize, MaxMessages - _counter);
            for (int i = 0; i < batchSize; i++)
            {
                _counter++;
                messages.Add(new SimpleMessage { Counter = _counter });
            }

            try
            {
                await _bus.PublishBatch(messages, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while publishing batch. Exception Type: {ExceptionType}", ex.GetType().Name);
            }

            await Task.Delay(TimeSpan.FromMilliseconds(delayInMs), stoppingToken);
        }

        if (_counter >= MaxMessages)
        {
            _logger.LogInformation("Reached maximum of {MaxMessages} messages. Worker stopping.", MaxMessages);
        }
    }
}
