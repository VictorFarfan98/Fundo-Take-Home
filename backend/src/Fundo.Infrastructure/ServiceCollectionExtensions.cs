using Fundo.Application.Submission;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Fundo.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFundoInfrastructure(this IServiceCollection services, string connectionString) =>
        services.AddDbContext<FundoDbContext>(options => options.UseSqlite(connectionString))
            .AddScoped<IApplicationStore, EfApplicationStore>();
}
