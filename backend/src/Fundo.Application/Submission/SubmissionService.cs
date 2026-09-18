using Fundo.Application.Rules;

namespace Fundo.Application.Submission;

public sealed class SubmissionService(IEnumerable<IApplicationRule> rules, IApplicationStore store)
{
    public async Task<ApplicationDecision> SubmitAsync(ApplicationSubmission submission, CancellationToken cancellationToken = default)
    {
        foreach (var rule in rules)
        {
            var decision = rule.Evaluate(submission);
            if (!decision.IsApproved)
                return decision;
        }

        var applicationId = await store.SaveApprovedApplicationAsync(submission, cancellationToken);
        return ApplicationDecision.Approve(applicationId);
    }
}
