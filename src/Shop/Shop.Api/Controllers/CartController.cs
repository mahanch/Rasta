using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.DTOs;
using Shop.Application.Features.Cart.Commands;
using Shop.Application.Features.Cart.Queries;

namespace Shop.Api.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly ISender _sender;

    public CartController(ISender sender)
    {
        _sender = sender;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetCart(CancellationToken ct)
    {
        var result = await _sender.Send(new GetCartQuery(GetUserId()), ct);
        return Ok(result.Value);
    }

    public record AddCartItemRequest(Guid ProductId, int Quantity);

    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] AddCartItemRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new AddItemToCartCommand(GetUserId(), request.ProductId, request.Quantity), ct);
        if (result.IsFailure)
        {
            return BadRequest(new { code = result.Error.Code, message = result.Error.Description });
        }

        return Ok(result.Value);
    }

    public record UpdateCartItemRequest(int Quantity);

    [HttpPut("items/{productId:guid}")]
    public async Task<IActionResult> UpdateQuantity(Guid productId, [FromBody] UpdateCartItemRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new UpdateCartItemQuantityCommand(GetUserId(), productId, request.Quantity), ct);
        if (result.IsFailure)
        {
            return BadRequest(new { code = result.Error.Code, message = result.Error.Description });
        }

        return Ok(result.Value);
    }

    [HttpDelete("items/{productId:guid}")]
    public async Task<IActionResult> RemoveItem(Guid productId, CancellationToken ct)
    {
        var result = await _sender.Send(new RemoveCartItemCommand(GetUserId(), productId), ct);
        if (result.IsFailure)
        {
            return BadRequest(new { code = result.Error.Code, message = result.Error.Description });
        }

        return Ok(result.Value);
    }

    [HttpDelete]
    public async Task<IActionResult> ClearCart(CancellationToken ct)
    {
        await _sender.Send(new ClearCartCommand(GetUserId()), ct);
        return Ok(new { message = "Cart cleared successfully." });
    }
}
