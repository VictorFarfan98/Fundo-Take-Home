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
    private static readonly HashSet<string> UsStateCodes =
    [
        "AL", "AK", "AZ", "AR", "CA", "CO", "CT", "DE", "FL", "GA", "HI", "ID", "IL", "IN", "IA", "KS", "KY", "LA", "ME", "MD", "MA", "MI", "MN", "MS", "MO", "MT", "NE", "NV", "NH", "NJ", "NM", "NY", "NC", "ND", "OH", "OK", "OR", "PA", "RI", "SC", "SD", "TN", "TX", "UT", "VT", "VA", "WA", "WV", "WI", "WY"
    ];

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
        if (!UsStateCodes.Contains(normalizedState))
            throw new ArgumentException("State must be a US state code.");
        if (requestedAmount <= 0)
            throw new ArgumentOutOfRangeException(nameof(requestedAmount), "Requested amount must be positive.");

        // Keep one canonical SSN for rules, lookups, and persistence.
        var normalizedSsn = string.Concat(ssn!.Where(static character => character is >= '0' and <= '9'));
        if (normalizedSsn.Length != 9)
            throw new ArgumentException("SSN must contain nine digits.", nameof(ssn));

        return new(
            firstName!.Trim(), lastName!.Trim(), address!.Trim(), normalizedState,
            companyName!.Trim(), requestedAmount, normalizedSsn);
    }
}
