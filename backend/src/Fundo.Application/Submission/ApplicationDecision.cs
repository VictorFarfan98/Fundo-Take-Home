namespace Fundo.Application.Submission;

public sealed record ApplicationDecision(bool IsApproved, string? Reason, Guid? ApplicationId = null)
{
    public static ApplicationDecision Approved { get; } = new(true, null);
    public static ApplicationDecision Approve(Guid applicationId) => new(true, null, applicationId);
    public static ApplicationDecision Denied(string reason) => new(false, reason);
}
