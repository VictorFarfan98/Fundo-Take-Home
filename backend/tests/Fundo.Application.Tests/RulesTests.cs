using Fundo.Application.Rules;
using Fundo.Application.Submission;
using Xunit;

namespace Fundo.Application.Tests;

public sealed class RulesTests
{
    private static readonly ApplicationSubmission ValidSubmission = ApplicationSubmission.Create(
        "Ada", "Lovelace", "1 Main St", "CA", "Fundo", 100m, "123-45-6789");

    [Fact]
    public void NyStateRule_denies_ny() =>
        Assert.Equal("state-not-supported", new NyStateRule().Evaluate(ValidSubmission with { State = "NY" }).Reason);

    [Fact]
    public void NyStateRule_approves_other_states() =>
        Assert.True(new NyStateRule().Evaluate(ValidSubmission).IsApproved);

    [Fact]
    public void Submission_rejects_unknown_state_codes() =>
        Assert.Throws<ArgumentException>(() => ApplicationSubmission.Create(
            "Ada", "Lovelace", "1 Main St", "ZZ", "Fundo", 100m, "123-45-6789"));

    [Fact]
    public void BlacklistedSsnRule_denies_listed_ssn() =>
        Assert.Equal("ssn-blacklisted", new BlacklistedSsnRule(["123456789"]).Evaluate(ValidSubmission).Reason);

    [Fact]
    public void BlacklistedSsnRule_approves_unlisted_ssn() =>
        Assert.True(new BlacklistedSsnRule(["111223333"]).Evaluate(ValidSubmission).IsApproved);

    [Fact]
    public async Task SubmissionService_approves_when_no_rule_denies()
    {
        var store = new RecordingStore();
        var decision = await new SubmissionService([new NyStateRule(), new BlacklistedSsnRule([])], store).SubmitAsync(ValidSubmission);

        Assert.True(decision.IsApproved);
        Assert.Equal(1, store.SaveCount);
    }

    [Fact]
    public async Task SubmissionService_stops_at_first_denial()
    {
        var first = new DenyingRule("first");
        var second = new DenyingRule("second");
        var decision = await new SubmissionService([first, second], new RecordingStore()).SubmitAsync(ValidSubmission);

        Assert.Equal("first", decision.Reason);
        Assert.Equal(0, second.EvaluationCount);
    }

    private sealed class RecordingStore : IApplicationStore
    {
        public int SaveCount { get; private set; }
        public Task<Guid> SaveApprovedApplicationAsync(ApplicationSubmission submission, CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(Guid.NewGuid());
        }
    }

    private sealed class DenyingRule(string reason) : IApplicationRule
    {
        public int EvaluationCount { get; private set; }
        public ApplicationDecision Evaluate(ApplicationSubmission submission)
        {
            EvaluationCount++;
            return ApplicationDecision.Denied(reason);
        }
    }
}
