using Fundo.Application.Submission;

namespace Fundo.Application.Rules;

public sealed class NyStateRule : IApplicationRule
{
    public ApplicationDecision Evaluate(ApplicationSubmission submission) =>
        submission.State == "NY"
            ? ApplicationDecision.Denied("state-not-supported")
            : ApplicationDecision.Approved;
}
