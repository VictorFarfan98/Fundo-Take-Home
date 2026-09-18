namespace Fundo.Application.Submission;

public interface IApplicationStore
{
    Task<Guid> SaveApprovedApplicationAsync(ApplicationSubmission submission, CancellationToken cancellationToken = default);
}
