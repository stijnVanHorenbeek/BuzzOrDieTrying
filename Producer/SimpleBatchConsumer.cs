using Contracts;
using MassTransit;

namespace Producer;

public class SimpleBatchConsumer : IConsumer<Batch<SimpleMessage>>
{
    private readonly IRequestClient<RequestMessage> _client;
    private readonly ILogger<SimpleBatchConsumer> _logger;

    public SimpleBatchConsumer(IRequestClient<RequestMessage> client, ILogger<SimpleBatchConsumer> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<Batch<SimpleMessage>> context)
    {
        _logger.LogInformation("Received batch of {Count} messages", context.Message.Length);

        var messages = context.Message.Select(m => m.Message).ToArray();
        var tasks = messages.Select(async message =>
        {
            try
            {
                var response = await _client.GetResponse<ResponseMessage>(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while requesting with counter: {Counter}. Exception Type: {ExceptionType}", message.Counter, ex.GetType().Name);
                throw;
            }
        });

        await Task.WhenAll(tasks);

        _logger.LogInformation("Completed processing batch of {Count} messages", context.Message.Length);
    }
}
