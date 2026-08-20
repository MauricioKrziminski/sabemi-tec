using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Sabemi.Payments.Api.Endpoints;
using Sabemi.Payments.Api.Middleware;
using Sabemi.Payments.Api.RealTime;
using Sabemi.Payments.Core.Processing;
using Sabemi.Payments.Core.Security;
using Sabemi.Payments.Infrastructure;
using Sabemi.Payments.Infrastructure.Persistence;
using Scalar.AspNetCore;

const string DashboardCorsPolicy = "dashboard";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));

builder.Services.AddOptions<WebhookSignatureOptions>()
    .Bind(builder.Configuration.GetSection(WebhookSignatureOptions.SectionName))
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.Secret),
        "O segredo de assinatura do webhook não foi configurado.")
    .ValidateOnStart();

builder.Services.AddSingleton<WebhookSignatureValidator>();

// O protocolo do SignalR tem serialização própria, separada da configurada para o HTTP.
builder.Services.AddSignalR().AddJsonProtocol(options =>
{
    options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

// Registrado antes da infraestrutura, que só define o notificador padrão se ainda não houver um.
builder.Services.AddSingleton<IPaymentEventNotifier, SignalRPaymentEventNotifier>();

builder.Services.AddPaymentsInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks().AddDbContextCheck<PaymentsDbContext>("postgres");

// O painel roda em outra origem, e o SignalR exige origem declarada junto com credenciais.
builder.Services.AddCors(options => options.AddPolicy(DashboardCorsPolicy, policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? ["http://localhost:3000"])
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

var app = builder.Build();

await ApplyMigrationsAsync(app);

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCorrelationId();
app.UseCors(DashboardCorsPolicy);

app.MapOpenApi();
app.MapScalarApiReference();

app.MapHealthChecks("/health/live").WithTags("Infraestrutura");
app.MapHealthChecks("/health/ready").WithTags("Infraestrutura");

app.MapWebhookEndpoints();
app.MapDashboardEndpoints();
app.MapHub<PaymentsHub>(PaymentsHub.Route);

app.Run();

// Em produção a migração seria um passo próprio do deploy. Aqui ela roda na inicialização
// para que um único "docker compose up" deixe o ambiente pronto.
static async Task ApplyMigrationsAsync(WebApplication app)
{
    await using var scope = app.Services.CreateAsyncScope();
    var database = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
    await database.Database.MigrateAsync();
}
