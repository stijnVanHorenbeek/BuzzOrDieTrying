namespace Contracts;

public record RequestMessage {
    public int Counter { get; init; }
}

public record SimpleMessage {
    public int Counter { get; init; }
}
