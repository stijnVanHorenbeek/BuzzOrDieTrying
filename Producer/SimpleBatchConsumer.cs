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

            try
            {
                var response = await _client.GetResponse<ResponseMessage>(new RequestMessage { Counter = counter }, context.CancellationToken);
            }
            catch (RequestTimeoutException ex)
            {
                _logger.LogError(ex, "Request timeout");
                throw;
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(ex, "Request cancelled");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while requesting with counter: {Counter}. Exception Type: {ExceptionType}", counter, ex.GetType().Name);
                throw;
            }
        }

        _logger.LogInformation("Completed processing batch of {Count} messages", context.Message.Length);
    }
}
