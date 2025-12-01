using MassTransit;
using Contracts;

namespace Consumer;

public class RequestMessageConsumer : IConsumer<RequestMessage>
{
    private readonly ILogger<RequestMessageConsumer> _logger;

    public RequestMessageConsumer(ILogger<RequestMessageConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<RequestMessage> context)
    {
        _logger.LogInformation("Received RequestMessage: {Text}", context.Message.Counter);

        throw CreateTestException();
    }

    static AggregateException CreateTestException(int value = 1000)
    {
        var inners = Enumerable.Range(0, value).Select(i => new Exception(new string('x', value))).ToArray();

        return new AggregateException("This is a test AggregateException", inners);
    }
}
