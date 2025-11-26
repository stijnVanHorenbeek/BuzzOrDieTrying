using Contracts;
using MassTransit;

namespace Producer;

public class Worker : BackgroundService
{
    private readonly IRequestClient<RequestMessage> _client;
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private int _counter = 0;

    public Worker(IRequestClient<RequestMessage> client, ILogger<Worker> logger, IConfiguration configuration)
    {
        _client = client;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _counter++;
            _logger.LogInformation("Sending request with counter: {Counter}", _counter);

            var handleErrors = _configuration.GetValue("Features:HandleErrors", true);

            if (handleErrors)
            {
                try
                {
                    var response = await _client.GetResponse<ResponseMessage>(new RequestMessage { Counter = _counter }, stoppingToken);

                    if (response.Message.HasError)
                    {
                        _logger.LogError("Received error response for counter {Counter}: {ErrorMessage}", _counter, response.Message.ErrorMessage);
                    }
                    else
                    {
                        _logger.LogInformation("Received response: {Result}", response.Message.Result);
                    }
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
            }
            else
            {
                _logger.LogWarning("Error handling DISABLED - exceptions will crash the producer");
                var response = await _client.GetResponse<ResponseMessage>(new RequestMessage { Counter = _counter }, stoppingToken);

                if (response.Message.HasError)
                {
                    _logger.LogError("Received error response for counter {Counter}: {ErrorMessage}", _counter, response.Message.ErrorMessage);
                }
                else
                {
                    _logger.LogInformation("Received response: {Result}", response.Message.Result);
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}
