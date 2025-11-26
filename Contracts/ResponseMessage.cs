namespace Contracts;

public record ResponseMessage {
    public string Result { get; init; } = default!;
    public bool HasError { get; init; }
    public string? ErrorMessage { get; init; }
}
