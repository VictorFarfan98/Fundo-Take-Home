namespace Fundo.Domain;

public sealed class LoanApplication
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required Guid CustomerId { get; init; }
    public required decimal RequestedAmount { get; set; }
}
