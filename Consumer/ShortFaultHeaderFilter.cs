using MassTransit;
using MassTransit.Transports;

namespace Consumer;

public class ShortFaultHeaderFilter : IFilter<ExceptionReceiveContext>
{
    private const int MaxStackTraceLength = 4000;
    private const int MaxMessageLength = 1024;

    public void Probe(ProbeContext context)
    {
        context.CreateScope("shortFaultHeaders");
    }

    public Task Send(ExceptionReceiveContext context, IPipe<ExceptionReceiveContext> next)
    {
        var ex = context.Exception;


        if (ex != null)
        {
            context.LogFaulted(ex);
            var msg = ex.Message ?? string.Empty;
            if (msg.Length > MaxMessageLength)
                msg = msg[..MaxMessageLength] + " ... [truncated]";

            var stack = ex.StackTrace ?? string.Empty;
            if (stack.Length > MaxStackTraceLength)
                stack = stack[..MaxStackTraceLength] + " ... [truncated]";

            context.ExceptionHeaders.Set("MT-Fault-Message", msg);
            context.ExceptionHeaders.Set("MT-Fault-ExceptionType", ex.GetType().FullName);
            context.ExceptionHeaders.Set("MT-Fault-StackTrace", stack);
        }

        return next.Send(context);
    }
}
