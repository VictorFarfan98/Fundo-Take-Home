using Fundo.Application.Submission;
using Microsoft.AspNetCore.Mvc;

namespace Fundo.Api.Controllers;

[ApiController]
[Route("api/applications")]
public sealed class ApplicationsController(SubmissionService submissionService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Submit(SubmitApplicationRequest request, CancellationToken cancellationToken)
    {
        ApplicationSubmission submission;
        try
        {
            submission = ApplicationSubmission.Create(request.FirstName, request.LastName, request.Address, request.State, request.CompanyName, request.RequestedAmount, request.Ssn);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { ["submission"] = [exception.Message] }));
        }

        try
        {
            var decision = await submissionService.SubmitAsync(submission, cancellationToken);
            return decision.IsApproved
                ? Ok(new { decision = "approved", applicationId = decision.ApplicationId })
                : UnprocessableEntity(new { decision = "denied", reason = decision.Reason });
        }
        catch
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Unable to submit application." });
        }
    }
}

public sealed record SubmitApplicationRequest(
    string? FirstName,
    string? LastName,
    string? Address,
    string? State,
    string? CompanyName,
    decimal RequestedAmount,
    string? Ssn);
