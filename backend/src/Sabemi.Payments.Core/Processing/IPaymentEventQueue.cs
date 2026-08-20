namespace Sabemi.Payments.Core.Processing;

/// <summary>
/// Sinal em memória que acorda o processamento em background.
///
/// A fila durável é a própria tabela de eventos brutos: se este sinal se perder, a varredura
/// periódica recupera o evento. Por isso a operação nunca bloqueia e pode falhar em silêncio.
/// </summary>
public interface IPaymentEventQueue
{
    bool TryEnqueue(Guid eventId);
}
