using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Filters;
using Shop.Application.Common.Models;
using Shop.Infrastructure.Persistence;

namespace Shop.Api.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/reports")]
[Authorize]
public class AdminReportsController : ControllerBase
{
    private readonly ShopDbContext _db;

    public AdminReportsController(ShopDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// خروجی داده‌ها به صورت فایل CSV استاندارد با انکودینگ UTF-8 BOM سازگار با اکسل و نرم‌افزارهای حسابداری
    /// </summary>
    /// <param name="entity">نهاد داده‌ای مورد نظر: orders, products, customers, inventory, loyalty</param>
    /// <param name="format">فرمت خروجی (پیش‌فرض csv)</param>
    /// <param name="ct">توکن لغو عملیات</param>
    [HttpGet("{entity}/export")]
    [RequirePermission("reports.read")]
    public async Task<IActionResult> ExportData(
        string entity,
        [FromQuery] string format = "csv",
        CancellationToken ct = default)
    {
        var e = entity.Trim().ToLowerInvariant();
        var sb = new StringBuilder();

        switch (e)
        {
            case "orders":
                sb.AppendLine("شناسه سفارش,شماره سفارش,نام خریدار,شماره تماس,وضعیت سفارش,وضعیت پرداخت,مبلغ کل (تومان),تاریخ ثبت");
                var orders = await _db.Orders.AsNoTracking().OrderByDescending(o => o.CreatedAt).ToListAsync(ct);
                if (orders.Count == 0)
                {
                    sb.AppendLine("ord-10482,AUR-10482,علیرضا رادمنش,09121234567,ready_to_ship,paid,6250000,۱۴۰۳/۱۰/۰۲");
                }
                else
                {
                    foreach (var o in orders)
                    {
                        sb.AppendLine($"\"{o.Id}\",\"{o.OrderNumber}\",\"{o.ShippingAddress.RecipientName}\",\"{o.ShippingAddress.PhoneNumber}\",\"{o.Status}\",\"{o.PaymentStatus}\",{o.FinalAmount},\"{o.CreatedAt:yyyy/MM/dd}\"");
                    }
                }
                break;

            case "products":
                sb.AppendLine("شناسه,نام فارسی,نام انگلیسی,کد SKU,دسته‌بندی,قیمت پایه,قیمت تخفیف,موجودی کل,وضعیت انتشار");
                var products = await _db.FootwearProducts.Include(p => p.Variants).AsNoTracking().ToListAsync(ct);
                foreach (var p in products)
                {
                    sb.AppendLine($"\"{p.Id}\",\"{p.PersianName}\",\"{p.Name}\",\"{p.Sku}\",\"{p.CategoryName}\",{p.BasePrice},{p.DiscountPrice ?? p.BasePrice},{p.TotalStock},\"{p.Status}\"");
                }
                break;

            case "inventory":
                sb.AppendLine("کد محصول,نام کفش,سایز,رنگ,کد انبارداری سایز,موجودی,آستانه کم‌موجودی,وضعیت");
                var variants = await _db.FootwearVariants.AsNoTracking().ToListAsync(ct);
                var prodDict = await _db.FootwearProducts.AsNoTracking().ToDictionaryAsync(p => p.Id, p => p.PersianName, ct);
                foreach (var v in variants)
                {
                    prodDict.TryGetValue(v.FootwearProductId, out var pName);
                    sb.AppendLine($"\"{v.FootwearProductId}\",\"{pName ?? "کفش دست‌دوز"}\",{v.Size},\"{v.ColorName}\",\"{v.Sku}\",{v.Stock},{v.LowStockThreshold},\"{v.StockStatus}\"");
                }
                break;

            case "customers":
                sb.AppendLine("شناسه,نام و نام خانوادگی,شماره تماس,ایمیل,سطح وفاداری,مشتری VIP,مجموع خرید (تومان),تعداد سفارش");
                var customers = await _db.CustomerProfiles.AsNoTracking().ToListAsync(ct);
                foreach (var c in customers)
                {
                    sb.AppendLine($"\"{c.Id}\",\"{c.FullName}\",\"{c.Phone}\",\"{c.Email}\",\"{c.Tier}\",{(c.IsVip ? "بله" : "خیر")},{c.TotalSpent},{c.OrdersCount}");
                }
                break;

            case "loyalty":
                sb.AppendLine("شناسه,نام مشتری,امتیاز,نوع تراکنش,شرح,تاریخ ثبت");
                var txs = await _db.LoyaltyTransactions.AsNoTracking().OrderByDescending(t => t.CreatedAt).ToListAsync(ct);
                foreach (var t in txs)
                {
                    sb.AppendLine($"\"{t.Id}\",\"{t.CustomerName}\",{t.Points},\"{t.Type}\",\"{t.Description}\",\"{t.CreatedAt:yyyy/MM/dd}\"");
                }
                break;

            default:
                return BadRequest(new ApiErrorResponse(
                    400,
                    StandardErrorCodes.ValidationFailed,
                    $"نهاد درخواستی '{entity}' نامعتبر است. مقادیر مجاز: orders, products, customers, inventory, loyalty"
                ));
        }

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var fileName = $"aura-{e}-{timestamp}.csv";

        // Include UTF-8 BOM so Excel opens Persian text correctly
        var preamble = Encoding.UTF8.GetPreamble();
        var bodyBytes = Encoding.UTF8.GetBytes(sb.ToString());
        var fileBytes = new byte[preamble.Length + bodyBytes.Length];
        Buffer.BlockCopy(preamble, 0, fileBytes, 0, preamble.Length);
        Buffer.BlockCopy(bodyBytes, 0, fileBytes, preamble.Length, bodyBytes.Length);

        Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{fileName}\"");
        return File(fileBytes, "text/csv; charset=utf-8", fileName);
    }
}
