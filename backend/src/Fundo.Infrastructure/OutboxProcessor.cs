using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Fundo.Infrastructure;

public sealed class OutboxProcessor(IServiceScopeFactory scopes) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessPendingAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }

    public async Task ProcessPendingAsync(CancellationToken cancellationToken = default)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FundoDbContext>();
        var client = scope.ServiceProvider.GetRequiredService<IExternalApplicationClient>();
        var messages = await db.OutboxMessages.Where(message => message.ProcessedAtUtc == null).ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            if (message.Operation == "update" && await db.OutboxMessages.AnyAsync(
                    prior => prior.ApplicationId == message.ApplicationId && prior.Operation == "create" && prior.ProcessedAtUtc == null,
                    cancellationToken))
                // Preserve create-before-update delivery for a new application.
                continue;

            try
            {
                await client.SendAsync(message, cancellationToken);
                message.ProcessedAtUtc = DateTimeOffset.UtcNow;
                message.LastError = null;
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                // Leave failures pending so the next poll can retry them.
                message.Attempts++;
                message.LastError = exception.GetType().Name;
            }

            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
