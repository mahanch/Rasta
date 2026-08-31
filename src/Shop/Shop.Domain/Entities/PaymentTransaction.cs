using Shop.Domain.Common;
using Shop.Domain.Entities;

namespace Shop.Domain.Entities;

public class PaymentTransaction : AggregateRoot<Guid>
{
    public Guid OrderId { get; private set; }
    public decimal Amount { get; private set; }
    public string GatewayName { get; private set; } = "MockGateway";
    public string TransactionReference { get; private set; } = string.Empty;
    public PaymentStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; private set; }

    private PaymentTransaction() { }

    public static PaymentTransaction Initiate(Guid orderId, decimal amount, string gatewayName = "MockGateway")
    {
        return new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Amount = amount,
            GatewayName = gatewayName,
            TransactionReference = $"TXN-{Guid.NewGuid():N}"[..20].ToUpperInvariant(),
            Status = PaymentStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void MarkSuccess(string? reference = null)
    {
        Status = PaymentStatus.Success;
        if (!string.IsNullOrWhiteSpace(reference)) TransactionReference = reference;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void MarkFailed(string errorMessage)
    {
        Status = PaymentStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTimeOffset.UtcNow;
    }
}
