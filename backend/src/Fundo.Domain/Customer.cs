namespace Fundo.Domain;

public sealed class Customer
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Address { get; set; }
    public required string State { get; set; }
    public required string CompanyName { get; set; }
    public required string NormalizedSsn { get; init; }
}
