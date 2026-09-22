using Shop.Domain.Common;

namespace Shop.Domain.Entities;

public class StoreSetting : Entity<Guid>
{
    // General
    public string StoreName { get; set; } = "چرم دست‌دوز اورا";
    public string Phone { get; set; } = "021-22003344";
    public string Email { get; set; } = "support@aura-leather.ir";
    public string Address { get; set; } = "تهران، خیابان ولیعصر، نرسیده به میدان تجریش";

    // Commerce
    public string Currency { get; set; } = "تومان";
    public decimal FreeShippingThreshold { get; set; } = 3000000;
    public decimal DefaultShippingFee { get; set; } = 85000;
    public decimal TaxPercent { get; set; } = 0;

    // Payments
    public bool ZarinpalActive { get; set; } = true;
    public string ZarinpalMerchantId { get; set; } = "00000000-0000-0000-0000-000000000000";
    public bool SamanActive { get; set; } = true;
    public string SamanTerminalId { get; set; } = "9823411";

    // SMS
    public string SmsProvider { get; set; } = "kavenegar";
    public string SmsApiKey { get; set; } = "AURA_DEMO_SMS_API_KEY_2026";

    // Inventory
    public int GlobalLowStockThreshold { get; set; } = 3;

    public StoreSetting() : base(Guid.NewGuid()) { }
}
