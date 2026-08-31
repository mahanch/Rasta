using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;
using Shop.Domain.ValueObjects;

namespace Shop.Infrastructure.License;

public class LicenseClientService : ILicenseClientService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<LicenseClientService> _logger;
    private readonly string _licenseKey;

    private LicenseState _cachedState;
    private readonly object _lock = new();

    public LicenseClientService(HttpClient httpClient, IConfiguration config, ILogger<LicenseClientService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
        _licenseKey = _config["LicenseSettings:LicenseKey"] ?? "SHOP-DEMO-LICENSE-KEY-2026";
        _cachedState = LicenseState.Default(_licenseKey);
    }

    public LicenseState GetCurrentLicenseState()
    {
        lock (_lock)
        {
            return _cachedState;
        }
    }

    public async Task<LicenseState> GetOrRefreshLicenseStateAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (!forceRefresh && (DateTimeOffset.UtcNow - _cachedState.LastCheckedAt).TotalMinutes < 5 && _cachedState.Status != "PendingVerification")
            {
                return _cachedState;
            }
        }

        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/licensing/verify", new { LicenseKey = _licenseKey }, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LicenseVerifyResponseDto>(cancellationToken: cancellationToken);
                if (result != null)
                {
                    var newState = new LicenseState(
                        result.LicenseKey,
                        result.Status,
                        result.Type,
                        result.MaxOrders,
                        result.UsedOrders,
                        result.RemainingOrders,
                        result.ExpirationDate,
                        result.IsValid,
                        DateTimeOffset.UtcNow
                    );

                    lock (_lock)
                    {
                        _cachedState = newState;
                    }

                    _logger.LogInformation("Successfully verified and cached store license. Type: {Type}, Valid: {IsValid}, Used: {Used}/{Max}",
                        newState.Type, newState.IsValid, newState.UsedOrders, newState.MaxOrders);

                    return newState;
                }
            }
            else
            {
                _logger.LogWarning("License server responded with status: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reach license server. Using cached fallback license state.");
        }

        lock (_lock)
        {
            return _cachedState;
        }
    }

    public async Task<bool> CanProcessOrderAsync(CancellationToken cancellationToken = default)
    {
        var state = await GetOrRefreshLicenseStateAsync(forceRefresh: false, cancellationToken);
        if (!state.IsValid)
        {
            return false;
        }

        if (state.Type == "OrderLimit" && state.MaxOrders.HasValue && state.UsedOrders >= state.MaxOrders.Value)
        {
            return false;
        }

        if (state.Type == "TimeLimit" && state.ExpirationDate.HasValue && state.ExpirationDate.Value < DateTimeOffset.UtcNow)
        {
            return false;
        }

        return true;
    }

    public async Task<bool> RecordOrderUsageAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/licensing/record-order", new { LicenseKey = _licenseKey }, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<RecordOrderResponseDto>(cancellationToken: cancellationToken);
                if (result != null)
                {
                    lock (_lock)
                    {
                        _cachedState = new LicenseState(
                            _cachedState.LicenseKey,
                            _cachedState.Status,
                            _cachedState.Type,
                            result.MaxOrders ?? _cachedState.MaxOrders,
                            result.UsedOrders,
                            result.RemainingOrders,
                            _cachedState.ExpirationDate,
                            _cachedState.IsValid,
                            DateTimeOffset.UtcNow
                        );
                    }

                    _logger.LogInformation("Order usage recorded remotely. New count: {Used}/{Max}", result.UsedOrders, result.MaxOrders);
                    return true;
                }
            }
            else
            {
                _logger.LogWarning("License Server rejected order usage recording with status: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording order usage to License Server. Incrementing local count as fallback.");
            lock (_lock)
            {
                var newUsed = _cachedState.UsedOrders + 1;
                int? newRemaining = _cachedState.MaxOrders.HasValue ? Math.Max(0, _cachedState.MaxOrders.Value - newUsed) : null;
                _cachedState = new LicenseState(
                    _cachedState.LicenseKey,
                    _cachedState.Status,
                    _cachedState.Type,
                    _cachedState.MaxOrders,
                    newUsed,
                    newRemaining,
                    _cachedState.ExpirationDate,
                    _cachedState.IsValid,
                    DateTimeOffset.UtcNow
                );
            }
        }

        return true;
    }

    private record LicenseVerifyResponseDto(
        bool IsValid,
        string LicenseKey,
        string Status,
        string Type,
        int? MaxOrders,
        int UsedOrders,
        int? RemainingOrders,
        DateTimeOffset? ExpirationDate,
        string Message
    );

    private record RecordOrderResponseDto(
        bool Success,
        string LicenseKey,
        int UsedOrders,
        int? MaxOrders,
        int? RemainingOrders,
        string Message
    );
}

public class LicenseSyncWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LicenseSyncWorker> _logger;

    public LicenseSyncWorker(IServiceProvider serviceProvider, ILogger<LicenseSyncWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting LicenseSyncWorker background service.");

        // Initial sync on startup
        await Task.Delay(3000, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var licenseService = scope.ServiceProvider.GetRequiredService<ILicenseClientService>();
                await licenseService.GetOrRefreshLicenseStateAsync(forceRefresh: true, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during periodic license sync.");
            }

            // Sync every 5 minutes
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
