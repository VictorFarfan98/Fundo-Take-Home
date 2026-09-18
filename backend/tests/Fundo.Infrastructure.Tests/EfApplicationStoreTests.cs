using Fundo.Application.Rules;
using Fundo.Application.Submission;
using Fundo.Api.Controllers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Net;
using System.Net.Http;
using Xunit;

namespace Fundo.Infrastructure.Tests;

public sealed class EfApplicationStoreTests
{
    [Fact]
    public async Task Approved_submission_creates_customer_application_and_create_outbox_snapshot()
    {
        await using var database = await TestDatabase.CreateAsync();
        var id = await database.Store.SaveApprovedApplicationAsync(Submission("123-45-6789"));

        var customer = await database.Context.Customers.SingleAsync();
        var application = await database.Context.LoanApplications.SingleAsync();
        var message = await database.Context.OutboxMessages.SingleAsync();
        Assert.Equal(id, application.Id);
        Assert.Equal("123456789", customer.NormalizedSsn);
        Assert.Equal("create", message.Operation);
        Assert.Contains(id.ToString(), message.Payload);
        Assert.Contains(customer.Id.ToString(), message.Payload);
    }

    [Fact]
    public async Task Returning_submission_updates_records_preserves_ids_and_emits_update()
    {
        await using var database = await TestDatabase.CreateAsync();
        var firstId = await database.Store.SaveApprovedApplicationAsync(Submission("123-45-6789"));
        var customerId = (await database.Context.Customers.SingleAsync()).Id;

        database.Context.ChangeTracker.Clear();
        var secondId = await database.Store.SaveApprovedApplicationAsync(Submission("123456789", "2 New St", "New Co", 250m));

        Assert.Equal(firstId, secondId);
        Assert.Equal(customerId, (await database.Context.Customers.SingleAsync()).Id);
        var application = await database.Context.LoanApplications.SingleAsync();
        Assert.Equal(250m, application.RequestedAmount);
        Assert.Equal("2 New St", (await database.Context.Customers.SingleAsync()).Address);
        Assert.Equal(["create", "update"], await database.Context.OutboxMessages.OrderBy(message => message.Operation).Select(message => message.Operation).ToArrayAsync());
        Assert.Equal(1, await database.Context.Customers.CountAsync());
        Assert.Equal(1, await database.Context.LoanApplications.CountAsync());
    }

    [Fact]
    public async Task Denials_leave_the_database_unchanged()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = new SubmissionService([new NyStateRule(), new BlacklistedSsnRule(["111223333"])], database.Store);

        Assert.Equal("state-not-supported", (await service.SubmitAsync(Submission("123456789") with { State = "NY" })).Reason);
        Assert.Equal("ssn-blacklisted", (await service.SubmitAsync(Submission("111223333"))).Reason);
        Assert.Equal(0, await database.Context.Customers.CountAsync());
        Assert.Equal(0, await database.Context.LoanApplications.CountAsync());
        Assert.Equal(0, await database.Context.OutboxMessages.CountAsync());
    }

    [Fact]
    public async Task Unique_constraints_reject_duplicate_customer_ssns_and_applications()
    {
        await using var database = await TestDatabase.CreateAsync();
        await database.Store.SaveApprovedApplicationAsync(Submission("123456789"));
        var customer = await database.Context.Customers.SingleAsync();
        database.Context.Customers.Add(new Fundo.Domain.Customer { FirstName = "Other", LastName = "Person", Address = "Elsewhere", State = "CA", CompanyName = "Other", NormalizedSsn = customer.NormalizedSsn });

        await Assert.ThrowsAsync<DbUpdateException>(() => database.Context.SaveChangesAsync());
        database.Context.ChangeTracker.Clear();
        database.Context.LoanApplications.Add(new Fundo.Domain.LoanApplication { CustomerId = customer.Id, RequestedAmount = 1m });
        await Assert.ThrowsAsync<DbUpdateException>(() => database.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task Outbox_failure_rolls_back_customer_and_application()
    {
        await using var database = await TestDatabase.CreateAsync();
        await database.Context.Database.ExecuteSqlRawAsync("CREATE TRIGGER fail_outbox BEFORE INSERT ON OutboxMessages BEGIN SELECT RAISE(FAIL, 'outbox failure'); END;");

        await Assert.ThrowsAsync<DbUpdateException>(() => database.Store.SaveApprovedApplicationAsync(Submission("123456789")));
        database.Context.ChangeTracker.Clear();
        Assert.Equal(0, await database.Context.Customers.CountAsync());
        Assert.Equal(0, await database.Context.LoanApplications.CountAsync());
        Assert.Equal(0, await database.Context.OutboxMessages.CountAsync());
    }

    [Fact]
    public async Task Submission_endpoint_returns_documented_approval_denial_and_validation_contracts()
    {
        await using var database = await TestDatabase.CreateAsync();
        var controller = new ApplicationsController(new SubmissionService([new NyStateRule(), new BlacklistedSsnRule(["111223333"])], database.Store));

        var approved = Assert.IsType<OkObjectResult>(await controller.Submit(Request("CA", "123-45-6789"), default));
        Assert.Contains("\"decision\":\"approved\"", JsonSerializer.Serialize(approved.Value));
        var stateDenial = Assert.IsType<UnprocessableEntityObjectResult>(await controller.Submit(Request("NY", "234-56-7890"), default));
        Assert.Contains("\"reason\":\"state-not-supported\"", JsonSerializer.Serialize(stateDenial.Value));
        var blacklistDenial = Assert.IsType<UnprocessableEntityObjectResult>(await controller.Submit(Request("CA", "111-22-3333"), default));
        Assert.Contains("\"reason\":\"ssn-blacklisted\"", JsonSerializer.Serialize(blacklistDenial.Value));
        Assert.IsType<BadRequestObjectResult>(await controller.Submit(Request("CA", "bad"), default));
    }

    [Fact]
    public async Task External_client_routes_create_and_update_by_application_id()
    {
        var handler = new RecordingHandler();
        var client = new ExternalApplicationClient(new HttpClient(handler) { BaseAddress = new Uri("http://mock/") });
        var applicationId = Guid.NewGuid();

        await client.SendAsync(Message("create", applicationId));
        await client.SendAsync(Message("update", applicationId));

        Assert.Equal(["POST /applications", $"PUT /applications/{applicationId}"], handler.Requests);
    }

    [Fact]
    public async Task Outbox_retries_failed_create_before_delivering_its_update_and_tolerates_duplicate_delivery()
    {
        await using var database = await TestDatabase.CreateAsync();
        var applicationId = await database.Store.SaveApprovedApplicationAsync(Submission("123456789"));
        await database.Store.SaveApprovedApplicationAsync(Submission("123456789", "2 New St"));
        var client = new RecordingClient(failFirstCreateAfterDelivery: true);
        await using var provider = database.CreateProvider(client);
        var processor = new OutboxProcessor(provider.GetRequiredService<IServiceScopeFactory>());

        await processor.ProcessPendingAsync();
        database.Context.ChangeTracker.Clear();
        Assert.Equal(1, (await database.Context.OutboxMessages.SingleAsync(message => message.Operation == "create")).Attempts);
        Assert.Null((await database.Context.OutboxMessages.SingleAsync(message => message.Operation == "update")).ProcessedAtUtc);

        await processor.ProcessPendingAsync();
        database.Context.ChangeTracker.Clear();
        Assert.All(await database.Context.OutboxMessages.ToListAsync(), message => Assert.NotNull(message.ProcessedAtUtc));
        Assert.Equal(["create", "create", "update"], client.Deliveries);
        Assert.Equal(applicationId, client.ApplicationIds.Last());
    }

    private static ApplicationSubmission Submission(string ssn, string address = "1 Main St", string company = "Fundo", decimal amount = 100m) =>
        ApplicationSubmission.Create("Ada", "Lovelace", address, "CA", company, amount, ssn);

    private static SubmitApplicationRequest Request(string state, string ssn) => new("Ada", "Lovelace", "1 Main St", state, "Fundo", 100m, ssn);

    private static OutboxMessage Message(string operation, Guid applicationId) => new()
    {
        CustomerId = Guid.NewGuid(), ApplicationId = applicationId, Operation = operation,
        Payload = JsonSerializer.Serialize(new { Application = new { Id = applicationId } })
    };

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<string> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add($"{request.Method} {request.RequestUri!.PathAndQuery}");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        }
    }

    private sealed class RecordingClient(bool failFirstCreateAfterDelivery) : IExternalApplicationClient
    {
        private bool _fail = failFirstCreateAfterDelivery;
        public List<string> Deliveries { get; } = [];
        public List<Guid> ApplicationIds { get; } = [];

        public Task SendAsync(OutboxMessage message, CancellationToken cancellationToken = default)
        {
            Deliveries.Add(message.Operation);
            ApplicationIds.Add(message.ApplicationId);
            if (_fail && message.Operation == "create")
            {
                _fail = false;
                throw new HttpRequestException("unavailable");
            }
            return Task.CompletedTask;
        }
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        public FundoDbContext Context { get; }
        public EfApplicationStore Store { get; }

        private TestDatabase(SqliteConnection connection, FundoDbContext context)
        {
            _connection = connection;
            Context = context;
            Store = new EfApplicationStore(context);
        }

        public static async Task<TestDatabase> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var context = new FundoDbContext(new DbContextOptionsBuilder<FundoDbContext>().UseSqlite(connection).Options);
            await context.Database.MigrateAsync();
            return new TestDatabase(connection, context);
        }

        public ServiceProvider CreateProvider(IExternalApplicationClient client) => new ServiceCollection()
            .AddDbContext<FundoDbContext>(options => options.UseSqlite(_connection))
            .AddScoped<IExternalApplicationClient>(_ => client)
            .BuildServiceProvider();

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
