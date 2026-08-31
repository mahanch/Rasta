using MediatR;
using Shop.Application.Common.Interfaces;
using Shop.Application.DTOs;
using Shop.Domain.Common;

namespace Shop.Application.Features.License.Queries;

public record GetShopLicenseStatusQuery() : IRequest<ShopLicenseStatusDto>;

public class GetShopLicenseStatusQueryHandler : IRequestHandler<GetShopLicenseStatusQuery, ShopLicenseStatusDto>
{
    private readonly ILicenseClientService _licenseService;

    public GetShopLicenseStatusQueryHandler(ILicenseClientService licenseService)
    {
        _licenseService = licenseService;
    }

    public async Task<ShopLicenseStatusDto> Handle(GetShopLicenseStatusQuery request, CancellationToken cancellationToken)
    {
        var state = await _licenseService.GetOrRefreshLicenseStateAsync(forceRefresh: false, cancellationToken);
        var msg = state.IsValid ? "Store license is active and valid." : $"Store license status is {state.Status}";

        return new ShopLicenseStatusDto(
            state.LicenseKey,
            state.Status,
            state.Type,
            state.MaxOrders,
            state.UsedOrders,
            state.RemainingOrders,
            state.ExpirationDate,
            state.IsValid,
            state.LastCheckedAt,
            msg
        );
    }
}

public record SyncShopLicenseCommand() : IRequest<ShopLicenseStatusDto>;

public class SyncShopLicenseCommandHandler : IRequestHandler<SyncShopLicenseCommand, ShopLicenseStatusDto>
{
    private readonly ILicenseClientService _licenseService;

    public SyncShopLicenseCommandHandler(ILicenseClientService licenseService)
    {
        _licenseService = licenseService;
    }

    public async Task<ShopLicenseStatusDto> Handle(SyncShopLicenseCommand request, CancellationToken cancellationToken)
    {
        var state = await _licenseService.GetOrRefreshLicenseStateAsync(forceRefresh: true, cancellationToken);
        var msg = state.IsValid ? "Store license synced successfully. License is active." : $"Store license synced. Status: {state.Status}";

        return new ShopLicenseStatusDto(
            state.LicenseKey,
            state.Status,
            state.Type,
            state.MaxOrders,
            state.UsedOrders,
            state.RemainingOrders,
            state.ExpirationDate,
            state.IsValid,
            state.LastCheckedAt,
            msg
        );
    }
}
