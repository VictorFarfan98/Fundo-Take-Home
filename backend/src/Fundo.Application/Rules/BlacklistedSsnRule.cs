using Fundo.Application.Submission;

namespace Fundo.Application.Rules;

public sealed class BlacklistedSsnRule(IEnumerable<string> normalizedSsns) : IApplicationRule
{
    private readonly HashSet<string> _normalizedSsns = new(normalizedSsns, StringComparer.Ordinal);

    public ApplicationDecision Evaluate(ApplicationSubmission submission) =>
        _normalizedSsns.Contains(submission.Ssn)
            ? ApplicationDecision.Denied("ssn-blacklisted")
            : ApplicationDecision.Approved;
}
