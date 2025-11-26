using Contracts;
using MassTransit;

namespace Producer;

public class SimpleBatchConsumer : IConsumer<Batch<SimpleMessage>>
{
    private readonly IRequestClient<RequestMessage> _client;
    private readonly ILogger<SimpleBatchConsumer> _logger;
    private readonly IConfiguration _configuration;

    public SimpleBatchConsumer(IRequestClient<RequestMessage> client, ILogger<SimpleBatchConsumer> logger, IConfiguration configuration)
    {
        _client = client;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task Consume(ConsumeContext<Batch<SimpleMessage>> context)
    {
        _logger.LogInformation("Received batch of {Count} messages", context.Message.Length);

        var handleErrors = _configuration.GetValue("Features:HandleErrors", true);

        foreach (var message in context.Message)
        {
            var counter = message.Message.Counter;
            _logger.LogInformation("Processing message from batch with counter: {Counter}", counter);

            if (handleErrors)
            {
                try
                {
                    var response = await _client.GetResponse<ResponseMessage>(new RequestMessage { Counter = counter }, context.CancellationToken);

                    if (response.Message.HasError)
                    {
                        _logger.LogError("Received error response for counter {Counter}: {ErrorMessage}", counter, response.Message.ErrorMessage);
                    }
                    else
                    {
                        _logger.LogInformation("Received response for counter {Counter}: {Result}", counter, response.Message.Result);
                    }
                }
                catch (RequestTimeoutException ex)
                {
                    _logger.LogError(ex, "Request timeout - possible RabbitMQ connection issue with counter: {Counter}", counter);
                }
                catch (OperationCanceledException ex)
                {
                    _logger.LogWarning(ex, "Request cancelled with counter: {Counter}", counter);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while requesting with counter: {Counter}. Exception Type: {ExceptionType}", counter, ex.GetType().Name);
                }
            }
            else
            {
                _logger.LogWarning("Error handling DISABLED - exceptions will crash the batch consumer");
                var response = await _client.GetResponse<ResponseMessage>(new RequestMessage { Counter = counter }, context.CancellationToken);

                if (response.Message.HasError)
                {
                    _logger.LogError("Received error response for counter {Counter}: {ErrorMessage}", counter, response.Message.ErrorMessage);
                }
                else
                {
                    _logger.LogInformation("Received response for counter {Counter}: {Result}", counter, response.Message.Result);
                }
            }
        }

        _logger.LogInformation("Completed processing batch of {Count} messages", context.Message.Length);
    }
}
