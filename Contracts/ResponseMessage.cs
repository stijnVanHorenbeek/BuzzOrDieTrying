namespace Contracts;

public record ResponseMessage {
    public string Result { get; init; } = default!;
}
