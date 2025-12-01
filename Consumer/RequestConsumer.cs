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

        var message = context.Message;

        if (message.Counter % 2 == 0)
        {
            throw CreateTestException();
        }

        await context.RespondAsync(new ResponseMessage
        {
            Result = message.Counter.ToString()
        });
    }

    static AggregateException CreateTestException(int value = 10)
    {
        var inners = Enumerable.Range(0, value).Select(i => new Exception(new string('x', 1000))).ToArray();

        return new AggregateException("This is a test AggregateException", inners);
    }
}
