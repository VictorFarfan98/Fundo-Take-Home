namespace Fundo.Infrastructure;

public sealed class OutboxMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required Guid CustomerId { get; init; }
    public required Guid ApplicationId { get; init; }
    public required string Operation { get; init; }
    public required string Payload { get; init; }
    public DateTimeOffset? ProcessedAtUtc { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
}
