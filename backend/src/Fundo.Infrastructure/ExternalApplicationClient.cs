using System.Text;

namespace Fundo.Infrastructure;

public sealed class ExternalApplicationClient(HttpClient httpClient) : IExternalApplicationClient
{
    public async Task SendAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        using var content = new StringContent(message.Payload, Encoding.UTF8, "application/json");
        using var response = message.Operation == "create"
            ? await httpClient.PostAsync("applications", content, cancellationToken)
            : await httpClient.PutAsync($"applications/{message.ApplicationId}", content, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
