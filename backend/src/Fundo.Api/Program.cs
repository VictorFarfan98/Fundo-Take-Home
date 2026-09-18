using Fundo.Application.Rules;
using Fundo.Application.Submission;
using Fundo.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AddFundoInfrastructure(
    builder.Configuration.GetConnectionString("Fundo") ?? "Data Source=fundo.db",
    builder.Configuration["ExternalServiceUrl"] ?? "http://localhost:3001/");
builder.Services.AddScoped<SubmissionService>();
builder.Services.AddSingleton<IApplicationRule, NyStateRule>();
builder.Services.AddSingleton<IApplicationRule>(_ => new BlacklistedSsnRule(builder.Configuration.GetSection("BlacklistedSsns").Get<string[]>() ?? []));

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI(options => options.RoutePrefix = string.Empty);
using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<FundoDbContext>().Database.Migrate();
app.MapControllers();

app.Run();

public partial class Program;
