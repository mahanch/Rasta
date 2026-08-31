using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.DTOs;
using Shop.Application.Features.Orders.Commands;
using Shop.Application.Features.Orders.Queries;
using Shop.Application.Features.Payments.Commands;
using Shop.Application.Features.License.Queries;
using Shop.Domain.Entities;

namespace Shop.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly ISender _sender;

    public OrdersController(ISender sender)
    {
        _sender = sender;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public record CheckoutRequest(AddressDto ShippingAddress, decimal DiscountAmount = 0);

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request, CancellationToken ct)
    {
        var command = new CheckoutOrderCommand(GetUserId(), request.ShippingAddress, request.DiscountAmount);
        var result = await _sender.Send(command, ct);

        if (result.IsFailure)
        {
            if (result.Error.Code.StartsWith("License."))
            {
                return StatusCode(403, new { code = result.Error.Code, message = result.Error.Description });
            }

            return BadRequest(new { code = result.Error.Code, message = result.Error.Description });
        }

        return Ok(result.Value);
    }

    [HttpGet("my-orders")]
    public async Task<IActionResult> GetMyOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetCustomerOrdersQuery(GetUserId(), page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var isAdmin = User.IsInRole("Admin");
        var result = await _sender.Send(new GetOrderByIdQuery(id, isAdmin ? null : GetUserId()), ct);

        if (result.IsFailure)
        {
            return NotFound(new { code = result.Error.Code, message = result.Error.Description });
        }

        return Ok(result.Value);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllAdmin([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetAdminOrdersQuery(status, page, pageSize), ct);
        return Ok(result);
    }

    public record UpdateStatusDto(OrderStatus Status);

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusDto dto, CancellationToken ct)
    {
        var result = await _sender.Send(new UpdateOrderStatusCommand(id, dto.Status), ct);
        if (result.IsFailure)
        {
            return BadRequest(new { code = result.Error.Code, message = result.Error.Description });
        }

        return Ok(new { message = $"Order status updated to {dto.Status} successfully." });
    }
}

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly ISender _sender;

    public PaymentsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("initiate/{orderId:guid}")]
    [Authorize]
    public async Task<IActionResult> Initiate(Guid orderId, [FromQuery] string gateway = "MockGateway", CancellationToken ct = default)
    {
        var result = await _sender.Send(new InitiatePaymentCommand(orderId, gateway), ct);
        if (result.IsFailure)
        {
            return BadRequest(new { code = result.Error.Code, message = result.Error.Description });
        }

        return Ok(result.Value);
    }

    public record MockSuccessRequest(string? TransactionReference);

    [HttpPost("mock-success/{orderId:guid}")]
    public async Task<IActionResult> MockSuccess(Guid orderId, [FromBody] MockSuccessRequest? request, CancellationToken ct)
    {
        var result = await _sender.Send(new SimulatePaymentSuccessCommand(orderId, request?.TransactionReference), ct);
        if (result.IsFailure)
        {
            return BadRequest(new { code = result.Error.Code, message = result.Error.Description });
        }

        return Ok(new { message = "Payment processed successfully. Order is now Paid and license usage is recorded." });
    }
}

[ApiController]
[Route("api/shop-license")]
public class ShopLicenseController : ControllerBase
{
    private readonly ISender _sender;

    public ShopLicenseController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var result = await _sender.Send(new GetShopLicenseStatusQuery(), ct);
        return Ok(result);
    }

    [HttpPost("sync")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Sync(CancellationToken ct)
    {
        var result = await _sender.Send(new SyncShopLicenseCommand(), ct);
        return Ok(result);
    }
}
