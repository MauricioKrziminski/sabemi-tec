using Microsoft.EntityFrameworkCore;
using Sabemi.Payments.Infrastructure;
using Sabemi.Payments.Infrastructure.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddPaymentsInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks().AddDbContextCheck<PaymentsDbContext>("postgres");

var app = builder.Build();

await ApplyMigrationsAsync(app);

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapOpenApi();
app.MapScalarApiReference();

app.MapHealthChecks("/health/live").WithTags("Infraestrutura");
app.MapHealthChecks("/health/ready").WithTags("Infraestrutura");

app.Run();

// Em produção a migração seria um passo próprio do deploy. Aqui ela roda na inicialização
// para que um único "docker compose up" deixe o ambiente pronto.
static async Task ApplyMigrationsAsync(WebApplication app)
{
    await using var scope = app.Services.CreateAsyncScope();
    var database = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
    await database.Database.MigrateAsync();
}
