namespace Fundo.Infrastructure;

public interface IExternalApplicationClient
{
    Task SendAsync(OutboxMessage message, CancellationToken cancellationToken = default);
}
