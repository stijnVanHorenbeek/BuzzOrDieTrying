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

        var value = context.Message.Counter;

        string result;
        if (value % 3 == 0 && value % 5 == 0)
        {
            throw CreateTestException(20);
        }
        if (value % 3 == 0)
        {
            result = "Fizz";
            _logger.LogInformation("Received message: {Value} (Fizz)", value);
        }
        else if (value % 5 == 0)
        {
            result = "Buzz";
            _logger.LogInformation("Received message: {Value} (Buzz)", value);
        }
        else
        {
            result = value.ToString();
            _logger.LogInformation("Received message: {Value} ({Result})", value, result);
        }

        var responseMessage = new ResponseMessage
        {
            Result = result
        };

        await context.RespondAsync(responseMessage);
    }

    static AggregateException CreateTestException(int value = 10)
    {
        var inners = Enumerable.Range(0, value).Select(i => new Exception(new string('x', 1000))).ToArray();

        return new AggregateException("This is a test AggregateException", inners);
    }
}
