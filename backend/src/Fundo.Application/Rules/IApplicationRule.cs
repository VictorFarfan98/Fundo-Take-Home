using Fundo.Application.Submission;

namespace Fundo.Application.Rules;

public interface IApplicationRule
{
    ApplicationDecision Evaluate(ApplicationSubmission submission);
}
