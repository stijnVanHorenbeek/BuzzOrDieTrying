using MassTransit;
using Microsoft.Extensions.Logging;
using Contracts;

namespace Consumer;

public class RequestMessageConsumer : IConsumer<RequestMessage>
{
    private readonly ILogger<RequestMessageConsumer> _logger;
    private readonly IConfiguration _configuration;

    public RequestMessageConsumer(ILogger<RequestMessageConsumer> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task Consume(ConsumeContext<RequestMessage> context)
    {
        _logger.LogInformation("Received RequestMessage: {Text}", context.Message.Counter);

        throw CreateTestException();
    }

    static AggregateException CreateTestException(int value = 5000)
    {
        var inners = Enumerable.Range(0, value).Select(i => new Exception($"Test Exception {i}")).ToArray();

        return new AggregateException("This is a test AggregateException", inners);
    }
}
