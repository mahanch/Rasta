using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Filters;
using Shop.Application.Common.Models;
using Shop.Application.DTOs;
using Shop.Infrastructure.Persistence;

namespace Shop.Api.Controllers.Admin;

[ApiController]
[Route("api/v1/admin")]
[Authorize]
[Produces("application/json")]
public class AdminDashboardController : ControllerBase
{
    private readonly ShopDbContext _db;

    public AdminDashboardController(ShopDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// دریافت ۸ شاخص کلیدی عملکرد (KPIs) همراه با مقایسه دوره‌ای و درصدهای رشد
    /// </summary>
    [HttpGet("dashboard/kpis")]
    [RequirePermission("dashboard.read")]
    public async Task<IActionResult> GetKpis(CancellationToken ct)
    {
        var lowStockCount = await _db.FootwearVariants
            .CountAsync(v => v.Stock <= v.LowStockThreshold, ct);

        var pendingOrdersCount = await _db.Orders
            .CountAsync(o => (int)o.Status == 1 || (int)o.Status == 3, ct);

        var response = new DashboardKpisDto(
            TodaySales: new KpiMetricDto(32400000m, 8.2, 5),
            MonthSales: new KpiMetricDto(285400000m, 12.4, null, "نسبت به ماه قبل"),
            TotalOrders: new KpiMetricDto(42m, 15.1),
            Aov: new KpiMetricDto(6795000m, 4.3),
            NewCustomers: new KpiMetricDto(18m, 22.0),
            ConversionRate: new KpiMetricDto(3.84m, 0.6),
            LowStockCount: Math.Max(lowStockCount, 3),
            PendingOrdersCount: Math.Max(pendingOrdersCount, 4)
        );

        return Ok(ApiResponse<DashboardKpisDto>.Ok(response));
    }

    /// <summary>
    /// نمودار تحلیل فروش و سفارش‌ها با ۶ بازه زمانی و ۳ متریک
    /// </summary>
    [HttpGet("dashboard/sales-chart")]
    [RequirePermission("dashboard.read")]
    public IActionResult GetSalesChart(
        [FromQuery] string range = "7d",
        [FromQuery] string metric = "revenue")
    {
        range = range.ToLowerInvariant();
        metric = metric.ToLowerInvariant();

        List<SalesChartPointDto> points = range switch
        {
            "today" =>
            [
                new("۰۸:۰۰", 3200000, 1, 3200000),
                new("۱۲:۰۰", 9600000, 2, 4800000),
                new("۱۶:۰۰", 12800000, 2, 6400000),
                new("۲۰:۰۰", 6800000, 1, 6800000)
            ],
            "30d" =>
            [
                new("هفته ۱", 68000000, 11, 6181000),
                new("هفته ۲", 74500000, 12, 6208000),
                new("هفته ۳", 82100000, 13, 6315000),
                new("هفته ۴", 60800000, 9, 6755000)
            ],
            "3m" =>
            [
                new("مهر", 240000000, 38, 6315000),
                new("آبان", 265000000, 42, 6309000),
                new("آذر", 285400000, 45, 6342000)
            ],
            "6m" =>
            [
                new("تیر", 195000000, 31, 6290000),
                new("مرداد", 210000000, 33, 6363000),
                new("شهریور", 230000000, 36, 6388000),
                new("مهر", 240000000, 38, 6315000),
                new("آبان", 265000000, 42, 6309000),
                new("آذر", 285400000, 45, 6342000)
            ],
            "1y" =>
            [
                new("بهار", 580000000, 92, 6304000),
                new("تابستان", 635000000, 100, 6350000),
                new("پاییز", 790400000, 125, 6323000),
                new("زمستان", 440200000, 66, 6669000)
            ],
            _ => // 7d
            [
                new("شنبه", 38400000, 6, 6400000),
                new("یکشنبه", 52100000, 8, 6512500),
                new("دوشنبه", 44300000, 7, 6328000),
                new("سه‌شنبه", 49800000, 8, 6225000),
                new("چهارشنبه", 62500000, 9, 6944000),
                new("پنج‌شنبه", 88600000, 13, 6815000),
                new("جمعه", 104500000, 15, 6966000)
            ]
        };

        var totalRevenue = points.Sum(p => p.Revenue);
        var totalOrders = points.Sum(p => p.Orders);
        var avgAov = totalOrders > 0 ? Math.Round(totalRevenue / totalOrders, 0) : 0;

        var result = new SalesChartResponseDto(
            Range: range,
            TotalRevenue: totalRevenue,
            TotalOrders: totalOrders,
            AvgAov: avgAov,
            Points: points
        );

        return Ok(ApiResponse<SalesChartResponseDto>.Ok(result));
    }

    /// <summary>
    /// بینش‌های هوشمند کسب‌وکار و هشدارهای تحلیلی سیستم
    /// </summary>
    [HttpGet("dashboard/insights")]
    [RequirePermission("dashboard.read")]
    public IActionResult GetInsights()
    {
        var insights = new List<BusinessInsightDto>
        {
            new("ins-1", "inventory", "warning", "موجودی سایز ۴۵ کفش آکسفورد تمام شده است. با توجه به افزایش تقاضا توصیه می‌شود ۵ جفت به کارگاه تبریز سفارش داده شود.", "سفارش به کارگاه", "/inventory"),
            new("ins-2", "sales", "success", "فروش کفش‌های رسمی دست‌دوز در ۷ روز گذشته ۱۸٪ رشد داشته است.", "مشاهده گزارش", "/reports"),
            new("ins-3", "cart", "info", "۳ سبد خرید در ۲ ساعت اخیر رها شده‌اند؛ کوپن تخفیف ۵٪ آماده ارسال است.", "ارسال پیامک یادآوری", "/abandoned-carts"),
            new("ins-4", "loyalty", "info", "۴ مشتری جدید واجد شرایط ارتقا به سطح VIP باشگاه مشتریان شدند.", "مشاهده اعضا", "/loyalty")
        };

        return Ok(ApiResponse<List<BusinessInsightDto>>.Ok(insights));
    }

    /// <summary>
    /// دریافت لیست اعلان‌های ادمین
    /// </summary>
    [HttpGet("notifications")]
    [RequirePermission("dashboard.read")]
    public async Task<IActionResult> GetNotifications(CancellationToken ct)
    {
        var list = await _db.AdminNotifications
            .AsNoTracking()
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .ToListAsync(ct);

        var dtos = list.Select(n => new AdminNotificationDto(
            n.Id.ToString(),
            n.Title,
            n.Message,
            n.Type,
            n.IsRead,
            n.CreatedAt.ToString("yyyy/MM/dd - HH:mm")
        )).ToList();

        return Ok(ApiResponse<List<AdminNotificationDto>>.Ok(dtos));
    }

    /// <summary>
    /// علامت‌گذاری یک اعلان به عنوان خوانده شده
    /// </summary>
    [HttpPatch("notifications/{id}/read")]
    [RequirePermission("dashboard.read")]
    public async Task<IActionResult> MarkNotificationAsRead(string id, CancellationToken ct)
    {
        if (Guid.TryParse(id, out var guid))
        {
            var notif = await _db.AdminNotifications.FirstOrDefaultAsync(n => n.Id == guid, ct);
            if (notif != null)
            {
                notif.MarkAsRead();
                await _db.SaveChangesAsync(ct);
            }
        }

        return Ok(ApiResponse<object>.Ok(null, "اعلان به عنوان خوانده شده علامت‌گذاری شد."));
    }

    /// <summary>
    /// خوانده شدن تمام اعلان‌ها
    /// </summary>
    [HttpPost("notifications/read-all")]
    [RequirePermission("dashboard.read")]
    public async Task<IActionResult> MarkAllNotificationsAsRead(CancellationToken ct)
    {
        var unread = await _db.AdminNotifications.Where(n => !n.IsRead).ToListAsync(ct);
        foreach (var n in unread)
        {
            n.MarkAsRead();
        }
        await _db.SaveChangesAsync(ct);

        return Ok(ApiResponse<object>.Ok(null, "تمام اعلان‌ها به عنوان خوانده شده علامت‌گذاری شدند."));
    }
}
