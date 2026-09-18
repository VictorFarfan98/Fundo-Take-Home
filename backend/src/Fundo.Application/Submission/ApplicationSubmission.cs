namespace Fundo.Application.Submission;

public sealed record ApplicationSubmission(
    string FirstName,
    string LastName,
    string Address,
    string State,
    string CompanyName,
    decimal RequestedAmount,
    string Ssn)
{
    public static ApplicationSubmission Create(
        string? firstName,
        string? lastName,
        string? address,
        string? state,
        string? companyName,
        decimal requestedAmount,
        string? ssn)
    {
        if (new[] { firstName, lastName, address, state, companyName, ssn }.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("All submission fields are required.");
        var normalizedState = state!.Trim().ToUpperInvariant();
        if (normalizedState.Length != 2 || !normalizedState.All(char.IsLetter))
            throw new ArgumentException("State must be a two-letter code.");
        if (requestedAmount <= 0)
            throw new ArgumentOutOfRangeException(nameof(requestedAmount), "Requested amount must be positive.");

        var normalizedSsn = string.Concat(ssn!.Where(static character => character is >= '0' and <= '9'));
        if (normalizedSsn.Length != 9)
            throw new ArgumentException("SSN must contain nine digits.", nameof(ssn));

        return new(
            firstName!.Trim(), lastName!.Trim(), address!.Trim(), normalizedState,
            companyName!.Trim(), requestedAmount, normalizedSsn);
    }
}
