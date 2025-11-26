using Contracts;
using MassTransit;

namespace Producer;

public class Worker : BackgroundService
{
    private readonly IRequestClient<RequestMessage> _client;
    private readonly ILogger<Worker> _logger;
    private int _counter = 0;

    public Worker(IRequestClient<RequestMessage> client, ILogger<Worker> logger)
    {
        _client = client;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _counter++;
            _logger.LogInformation("Sending request with counter: {Counter}", _counter);

            try
            {
                var response = await _client.GetResponse<ResponseMessage>(new RequestMessage { Counter = _counter }, stoppingToken);
                _logger.LogInformation("Received response: {Result}", response.Message.Result);
            }
            catch (RequestTimeoutException ex)
            {
                _logger.LogError(ex, "Request timeout - possible RabbitMQ connection issue with counter: {Counter}", _counter);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(ex, "Request cancelled with counter: {Counter}", _counter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while requesting with counter: {Counter}. Exception Type: {ExceptionType}", _counter, ex.GetType().Name);
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}
