using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sabemi.Payments.Core.Contracts;
using Sabemi.Payments.Core.Processing;
using Sabemi.Payments.Core.Validation;
using Sabemi.Payments.Infrastructure.Ingestion;
using Sabemi.Payments.Infrastructure.Persistence;
using Sabemi.Payments.Infrastructure.Processing;
using Sabemi.Payments.Infrastructure.Queries;

namespace Sabemi.Payments.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public const string ConnectionStringName = "Payments";

    public static IServiceCollection AddPaymentsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"A connection string '{ConnectionStringName}' não foi configurada.");

        // A fábrica permite abrir um contexto novo em pontos onde o rastreador de mudanças
        // não pode ser reaproveitado, como no tratamento de violação de chave única.
        services.AddDbContextFactory<PaymentsDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention());

        services.AddScoped(provider =>
            provider.GetRequiredService<IDbContextFactory<PaymentsDbContext>>().CreateDbContext());

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IValidator<PaymentWebhookRequest>, PaymentWebhookRequestValidator>();

        services.AddSingleton<ChannelPaymentEventQueue>();
        services.AddSingleton<IPaymentEventQueue>(provider =>
            provider.GetRequiredService<ChannelPaymentEventQueue>());

        // A API troca esta implementação pela que publica no hub SignalR.
        services.TryAddSingleton<IPaymentEventNotifier, NullPaymentEventNotifier>();

        services.AddScoped<WebhookIngestionService>();

        services.AddOptions<ProcessingOptions>()
            .Bind(configuration.GetSection(ProcessingOptions.SectionName));

        services.AddScoped<PaymentQueryService>();
        services.AddScoped<PaymentReprocessService>();

        services.AddSingleton<PaymentEventProcessor>();
        services.AddHostedService<PaymentProcessingWorker>();
        services.AddHostedService<PaymentRecoveryWorker>();

        return services;
    }
}
