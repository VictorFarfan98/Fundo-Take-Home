using System.Text.Json;
using Fundo.Application.Submission;
using Fundo.Domain;
using Microsoft.EntityFrameworkCore;

namespace Fundo.Infrastructure;

public sealed class EfApplicationStore(FundoDbContext db) : IApplicationStore
{
    public async Task<Guid> SaveApprovedApplicationAsync(ApplicationSubmission submission, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var customer = await db.Customers.Include(customer => customer.Application)
            .SingleOrDefaultAsync(customer => customer.NormalizedSsn == submission.Ssn, cancellationToken);
        var operation = customer is null ? "create" : "update";

        if (customer is null)
        {
            customer = new Customer { FirstName = submission.FirstName, LastName = submission.LastName, Address = submission.Address, State = submission.State, CompanyName = submission.CompanyName, NormalizedSsn = submission.Ssn };
            db.Customers.Add(customer);
            customer.Application = new LoanApplication { CustomerId = customer.Id, RequestedAmount = submission.RequestedAmount };
        }
        else
        {
            customer.FirstName = submission.FirstName;
            customer.LastName = submission.LastName;
            customer.Address = submission.Address;
            customer.State = submission.State;
            customer.CompanyName = submission.CompanyName;
            customer.Application!.RequestedAmount = submission.RequestedAmount;
        }

        var application = customer.Application!;
        db.OutboxMessages.Add(new OutboxMessage
        {
            CustomerId = customer.Id,
            ApplicationId = application.Id,
            Operation = operation,
            Payload = JsonSerializer.Serialize(new
            {
                Customer = new { customer.Id, customer.FirstName, customer.LastName, customer.Address, customer.State, customer.CompanyName },
                Application = new { application.Id, application.CustomerId, application.RequestedAmount }
            })
        });

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return application.Id;
    }
}
