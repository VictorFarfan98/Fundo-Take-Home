using Fundo.Application.Submission;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Fundo.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFundoInfrastructure(this IServiceCollection services, string connectionString, string externalServiceUrl)
    {
        services.AddDbContext<FundoDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IApplicationStore, EfApplicationStore>();
        services.AddHttpClient<IExternalApplicationClient, ExternalApplicationClient>(client => client.BaseAddress = new Uri(externalServiceUrl));
        services.AddHostedService<OutboxProcessor>();
        return services;
    }
}
