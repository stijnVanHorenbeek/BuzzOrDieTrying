using MassTransit;
using Microsoft.Extensions.Logging;
using Contracts;

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
        var n = 3;
        var m = 5;

        string result;
        if (value % 3 == 0 && value % 5 == 0)
        {
            result = "FizzBuzz";
            _logger.LogInformation("Received message: {Value} ({Result})", value, result);
            _logger.LogWarning("Throwing exception for value '{Value}' to test retry mechanism.", value);
            throw new Exception("Test exception for retry mechanism.");
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

        var response = new ResponseMessage
        {
            Result = result
        };

        await context.RespondAsync(response);
    }
}
