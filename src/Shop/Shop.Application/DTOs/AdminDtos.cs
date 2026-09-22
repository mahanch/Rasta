using System.Text.Json.Serialization;

namespace Shop.Application.DTOs;

#region Auth & RBAC DTOs

public record AdminLoginRequest(string Email, string Password);

public record AdminUserDto(
    string Id,
    string Name,
    string Email,
    string Role,
    string RoleNameFa,
    List<string> Permissions
);

public record AdminLoginResponse(
    string Token,
    string RefreshToken,
    int ExpiresIn,
    AdminUserDto User
);

public record CreateAdminUserRequest(
    string Name,
    string Email,
    string Role
);

public record RoleDto(
    string Id,
    string Name,
    string NameFa,
    string Description,
    List<string> Permissions
);

public record UpdateRolePermissionsRequest(
    List<string> Permissions
);

#endregion

#region Dashboard DTOs

public record KpiMetricDto(
    decimal Value,
    double GrowthPercent,
    int? OrdersCount = null,
    string? ComparedPeriod = null
);

public record DashboardKpisDto(
    KpiMetricDto TodaySales,
    KpiMetricDto MonthSales,
    KpiMetricDto TotalOrders,
    KpiMetricDto Aov,
    KpiMetricDto NewCustomers,
    KpiMetricDto ConversionRate,
    int LowStockCount,
    int PendingOrdersCount
);

public record SalesChartPointDto(
    string Label,
    decimal Revenue,
    int Orders,
    decimal Aov
);

public record SalesChartResponseDto(
    string Range,
    decimal TotalRevenue,
    int TotalOrders,
    decimal AvgAov,
    List<SalesChartPointDto> Points
);

public record BusinessInsightDto(
    string Id,
    string Type,
    string Severity,
    string Message,
    string? ActionText = null,
    string? ActionUrl = null
);

public record AdminNotificationDto(
    string Id,
    string Title,
    string Message,
    string Type,
    bool IsRead,
    string CreatedAt
);

#endregion

#region Footwear Products DTOs

public record FootwearSpecsDto(
    string Material,
    string LeatherType,
    string Tannery,
    string SoleMaterial,
    string Construction,
    string Origin
);

public record FootwearSeoDto(
    string Title,
    string MetaDescription,
    string CanonicalUrl,
    string FocusKeyword
);

public record FootwearVariantDto(
    string Id,
    int Size,
    string ColorName,
    string ColorHex,
    string Sku,
    int Stock,
    int LowStockThreshold,
    string StockStatus
);

public record FootwearProductDto(
    string Id,
    string PersianName,
    string Name,
    string Slug,
    string Sku,
    string Category,
    string CategoryName,
    string Collection,
    string Gender,
    decimal BasePrice,
    decimal? DiscountPrice,
    decimal CostPrice,
    FootwearSpecsDto Specs,
    List<FootwearVariantDto> Variants,
    List<string> Images,
    string ShortDescription,
    string FullDescription,
    List<string> CareInstructions,
    string Status,
    FootwearSeoDto Seo,
    int TotalStock,
    string StockStatus,
    string CreatedAt
);

public record CreateFootwearProductRequest(
    string PersianName,
    string Name,
    string Slug,
    string Sku,
    string Category,
    string CategoryName,
    string Collection,
    string Gender,
    decimal BasePrice,
    decimal? DiscountPrice,
    decimal CostPrice,
    FootwearSpecsDto Specs,
    List<CreateFootwearVariantRequest> Variants,
    List<string> Images,
    string ShortDescription,
    string FullDescription,
    List<string> CareInstructions,
    string Status,
    FootwearSeoDto Seo
);

public record CreateFootwearVariantRequest(
    int Size,
    string ColorName,
    string ColorHex,
    string Sku,
    int Stock,
    int LowStockThreshold = 3
);

public record UpdateFootwearProductRequest(
    string PersianName,
    string Name,
    string Slug,
    string Sku,
    string Category,
    string CategoryName,
    string Collection,
    string Gender,
    decimal BasePrice,
    decimal? DiscountPrice,
    decimal CostPrice,
    FootwearSpecsDto Specs,
    List<CreateFootwearVariantRequest>? Variants,
    List<string> Images,
    string ShortDescription,
    string FullDescription,
    List<string> CareInstructions,
    string Status,
    FootwearSeoDto Seo
);

public record BulkProductActionRequest(
    string Action, // "publish" | "draft" | "delete"
    List<string> ProductIds
);

#endregion

#region Footwear Inventory DTOs

public record InventoryMatrixVariantDto(
    string VariantId,
    int Size,
    string ColorName,
    string ColorHex,
    string Sku,
    int Stock,
    int LowStockThreshold,
    string Status
);

public record InventoryMatrixItemDto(
    string ProductId,
    string PersianName,
    string Name,
    string Sku,
    string CategoryName,
    int TotalStock,
    string StockStatus,
    List<InventoryMatrixVariantDto> Variants
);

public record UpdateVariantStockRequest(
    int NewStock,
    string Reason
);

public record UpdateThresholdRequest(
    int LowStockThreshold
);

#endregion

#region Orders DTOs

public record AdminCustomerSummaryDto(
    string Id,
    string Name,
    string Phone,
    string Email,
    bool IsVip,
    string Tier
);

public record AdminOrderItemDto(
    string ProductId,
    string ProductName,
    string ProductImage,
    string Sku,
    int Size,
    string Color,
    decimal Price,
    int Quantity
);

public record AdminShippingAddressDto(
    string Recipient,
    string Phone,
    string Province,
    string City,
    string PostalCode,
    string Address
);

public record OrderTimelineItemDto(
    string Id,
    string Status,
    string Title,
    string Description,
    string Timestamp,
    string PerformedBy
);

public record AdminOrderDetailsDto(
    string Id,
    string OrderNumber,
    AdminCustomerSummaryDto Customer,
    string Date,
    string Status,
    string PaymentStatus,
    string PaymentMethod,
    List<AdminOrderItemDto> Items,
    decimal Subtotal,
    decimal ShippingFee,
    decimal DiscountAmount,
    decimal TotalAmount,
    AdminShippingAddressDto ShippingAddress,
    string ShippingMethod,
    string TrackingCode,
    List<OrderTimelineItemDto> Timeline,
    string Notes
);

public record UpdateAdminOrderStatusRequest(
    string NewStatus,
    string Note,
    string? TrackingCode = null
);

#endregion

#region Returns DTOs

public record ReturnRequestDto(
    string Id,
    string OrderId,
    string OrderNumber,
    string CustomerName,
    string CustomerPhone,
    string Reason,
    int RequestedSize,
    string SoleCondition,
    string LeatherCondition,
    string Status,
    string AdminNotes,
    decimal RefundAmount,
    string CreatedAt
);

public record UpdateReturnStatusRequest(
    string Status,
    string AdminNotes,
    decimal? RefundAmount = null
);

#endregion

#region Abandoned Carts DTOs

public record AbandonedCartDto(
    string Id,
    string CartId,
    string CustomerName,
    string CustomerPhone,
    decimal TotalAmount,
    int ItemCount,
    string Status,
    string? CouponSent,
    double IdleHours,
    string LastActive
);

public record SendAbandonedCartReminderRequest(
    string CouponCode,
    int DiscountPercent
);

#endregion

#region CRM 360 & Customers DTOs

public record CustomerListItemDto(
    string Id,
    string FullName,
    string Phone,
    string Email,
    string Tier,
    bool IsVip,
    decimal TotalSpent,
    int OrdersCount,
    string LastOrderDate
);

public record CustomerNoteDto(
    string Id,
    string AdminName,
    string Note,
    string CreatedAt
);

public record CustomerProfile360Dto(
    string Id,
    string FullName,
    string Phone,
    string Email,
    bool IsVip,
    string Tier,
    decimal TotalSpent,
    int OrdersCount,
    decimal WalletBalance,
    int LoyaltyPoints,
    int? PreferredSize,
    string PreferredColors,
    List<CustomerNoteDto> Notes,
    List<AdminOrderDetailsDto> RecentOrders
);

public record AddCustomerNoteRequest(
    string Note
);

public record CustomerSegmentDto(
    string Id,
    string Name,
    string Description,
    string Criteria,
    int CustomerCount,
    string CreatedAt
);

public record CreateCustomerSegmentRequest(
    string Name,
    string Description,
    string Criteria
);

#endregion

#region Loyalty DTOs

public record LoyaltyRulesDto(
    int PurchaseRatio,
    int RegisterPoints,
    int ReviewPoints,
    int ReferralPoints,
    int PointExpiryDays
);

public record LoyaltyTierDto(
    string Id,
    string TierKey,
    string NameFa,
    decimal MinSpend,
    int MinPoints,
    int PermanentDiscount,
    List<string> Perks
);

public record UpdateLoyaltyTierRequest(
    decimal MinSpend,
    int MinPoints,
    int PermanentDiscount,
    List<string> Perks
);

public record LoyaltyRewardDto(
    string Id,
    string Title,
    int PointCost,
    string Description,
    string DiscountType,
    decimal DiscountValue,
    int ExpirationDays,
    bool IsActive
);

public record CreateLoyaltyRewardRequest(
    string Title,
    int PointCost,
    string Description,
    string DiscountType,
    decimal DiscountValue,
    int ExpirationDays
);

public record LoyaltyTransactionDto(
    string Id,
    string CustomerId,
    string CustomerName,
    int Points,
    string Type,
    string Description,
    string CreatedAt
);

#endregion

#region Marketing & Coupons & Campaigns DTOs

public record CouponDto(
    string Id,
    string Code,
    string Type,
    decimal Value,
    decimal? MaxDiscount,
    decimal MinCartSpend,
    string StartDate,
    string EndDate,
    int TotalLimit,
    int UsedCount,
    int PerUserLimit,
    List<string> TargetTiers,
    bool IsActive
);

public record CreateCouponRequest(
    string Code,
    string Type,
    decimal Value,
    decimal? MaxDiscount,
    decimal MinCartSpend,
    string StartDate,
    string EndDate,
    int TotalLimit,
    int PerUserLimit,
    List<string> TargetTiers
);

public record CampaignDto(
    string Id,
    string Title,
    string StartDate,
    string EndDate,
    int ClicksCount,
    int OrdersCount,
    decimal Revenue,
    decimal SpendCost,
    double Roi,
    bool IsActive
);

#endregion

#region CMS & Blog DTOs

public record AdminBlogArticleDto(
    string Id,
    string Title,
    string Slug,
    string Excerpt,
    string Author,
    string Category,
    string Status,
    string CoverImage,
    string MetaTitle,
    string MetaDescription,
    string FocusKeyword,
    List<string> RelatedProducts,
    string Content,
    string CreatedAt
);

public record CreateOrUpdateBlogArticleRequest(
    string Title,
    string Slug,
    string Excerpt,
    string Author,
    string Category,
    string Status,
    string CoverImage,
    string MetaTitle,
    string MetaDescription,
    string FocusKeyword,
    List<string> RelatedProducts,
    string Content
);

public record CmsHomepageBlockDto(
    string Id,
    string Type,
    bool IsVisible,
    int Order
);

public record UpdateHomepageBlocksRequest(
    List<CmsHomepageBlockDto> Blocks
);

#endregion

#region SEO DTOs

public record SeoIssueDto(
    string Id,
    string EntityType,
    string EntityName,
    string Url,
    string IssueType,
    string Severity,
    string Recommendation
);

public record SeoAuditResponseDto(
    int HealthScore,
    int MissingTitlesCount,
    int MissingDescriptionsCount,
    int MissingAltCount,
    int BrokenLinksCount,
    List<SeoIssueDto> Issues
);

public record SeoRedirectDto(
    string Id,
    string FromUrl,
    string ToUrl,
    int Type,
    string CreatedAt
);

public record CreateSeoRedirectRequest(
    string FromUrl,
    string ToUrl,
    int Type
);

public record RobotsTxtDto(
    string Content
);

public record UpdateRobotsTxtRequest(
    string Content
);

#endregion

#region Audit Log & Settings DTOs

public record AuditLogDto(
    string Id,
    string AdminName,
    string AdminRole,
    string Action,
    string Entity,
    string BeforeValue,
    string AfterValue,
    string Ip,
    string Timestamp
);

public record StoreGeneralSettingsDto(
    string StoreName,
    string Phone,
    string Email,
    string Address
);

public record StoreCommerceSettingsDto(
    string Currency,
    decimal FreeShippingThreshold,
    decimal DefaultShippingFee,
    decimal TaxPercent
);

public record GatewaySettingsDto(
    bool Active,
    string? MerchantId = null,
    string? TerminalId = null
);

public record StorePaymentsSettingsDto(
    GatewaySettingsDto Zarinpal,
    GatewaySettingsDto Saman
);

public record StoreSmsSettingsDto(
    string Provider,
    string ApiKey
);

public record StoreSettingsDto(
    StoreGeneralSettingsDto General,
    StoreCommerceSettingsDto Commerce,
    StorePaymentsSettingsDto Payments,
    StoreSmsSettingsDto Sms
);

#endregion
