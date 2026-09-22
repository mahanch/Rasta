using Shop.Domain.Common;

namespace Shop.Domain.Entities;

public class ReturnRequest : Entity<Guid>
{
    public Guid OrderId { get; private set; }
    public string OrderNumber { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;
    public string CustomerPhone { get; private set; } = string.Empty;

    public string Reason { get; private set; } = string.Empty; // "سایز", "دوخت", "رنگ", etc.
    public int RequestedSize { get; private set; }
    public string SoleCondition { get; private set; } = string.Empty; // ارزیابی سلامت زیره
    public string LeatherCondition { get; private set; } = string.Empty; // ارزیابی سلامت چرم
    
    public string Status { get; private set; } = "registered"; // "registered" | "inspecting" | "approved" | "rejected" | "item_received" | "refunded"
    public string AdminNotes { get; private set; } = string.Empty;
    public decimal RefundAmount { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; private set; }

    private ReturnRequest() { }

    public ReturnRequest(
        Guid orderId,
        string orderNumber,
        Guid customerId,
        string customerName,
        string customerPhone,
        string reason,
        int requestedSize,
        string soleCondition,
        string leatherCondition,
        decimal refundAmount,
        string status = "registered",
        string adminNotes = "",
        Guid? id = null) : base(id ?? Guid.NewGuid())
    {
        OrderId = orderId;
        OrderNumber = orderNumber;
        CustomerId = customerId;
        CustomerName = customerName;
        CustomerPhone = customerPhone;
        Reason = reason;
        RequestedSize = requestedSize;
        SoleCondition = soleCondition;
        LeatherCondition = leatherCondition;
        RefundAmount = refundAmount;
        Status = status;
        AdminNotes = adminNotes;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateStatus(string status, string adminNotes, decimal? refundAmount = null)
    {
        Status = status;
        if (!string.IsNullOrWhiteSpace(adminNotes)) AdminNotes = adminNotes;
        if (refundAmount.HasValue) RefundAmount = refundAmount.Value;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
