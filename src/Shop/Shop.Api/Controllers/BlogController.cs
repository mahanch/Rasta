using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Features.Blog.Commands;
using Shop.Application.Features.Blog.Queries;

namespace Shop.Api.Controllers;

[ApiController]
[Route("api/blog")]
public class BlogController : ControllerBase
{
    private readonly ISender _sender;

    public BlogController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("posts")]
    public async Task<IActionResult> GetPosts(
        [FromQuery] string? search,
        [FromQuery] Guid? categoryId,
        [FromQuery] string? tag,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var isAdmin = User.IsInRole("Admin");
        var result = await _sender.Send(new GetBlogPostsQuery(search, categoryId, tag, isAdmin, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("posts/{slug}")]
    public async Task<IActionResult> GetPostBySlug(string slug, CancellationToken ct)
    {
        var isAdmin = User.IsInRole("Admin");
        var result = await _sender.Send(new GetBlogPostBySlugQuery(slug, isAdmin), ct);
        if (result.IsFailure)
        {
            return NotFound(new { code = result.Error.Code, message = result.Error.Description });
        }

        return Ok(result.Value);
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(CancellationToken ct)
    {
        var result = await _sender.Send(new GetBlogCategoriesQuery(), ct);
        return Ok(result);
    }

    public record CreateBlogPostRequest(
        string Title,
        string Slug,
        string Summary,
        string Content,
        Guid? CategoryId,
        string? CoverImageUrl,
        List<string>? Tags,
        bool PublishImmediately = true
    );

    [HttpPost("posts")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreatePost([FromBody] CreateBlogPostRequest request, CancellationToken ct)
    {
        var authorId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var authorName = User.FindFirstValue(ClaimTypes.Name) ?? "Admin";

        var command = new CreateBlogPostCommand(
            request.Title,
            request.Slug,
            request.Summary,
            request.Content,
            authorId,
            authorName,
            request.CategoryId,
            request.CoverImageUrl,
            request.Tags,
            request.PublishImmediately
        );

        var result = await _sender.Send(command, ct);
        if (result.IsFailure)
        {
            return BadRequest(new { code = result.Error.Code, message = result.Error.Description });
        }

        return CreatedAtAction(nameof(GetPostBySlug), new { slug = request.Slug }, new { id = result.Value });
    }

    public record UpdateBlogPostRequest(
        string Title,
        string Slug,
        string Summary,
        string Content,
        Guid? CategoryId,
        string? CoverImageUrl,
        List<string>? Tags,
        bool IsPublished
    );

    [HttpPut("posts/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdatePost(Guid id, [FromBody] UpdateBlogPostRequest request, CancellationToken ct)
    {
        var command = new UpdateBlogPostCommand(
            id,
            request.Title,
            request.Slug,
            request.Summary,
            request.Content,
            request.CategoryId,
            request.CoverImageUrl,
            request.Tags,
            request.IsPublished
        );

        var result = await _sender.Send(command, ct);
        if (result.IsFailure)
        {
            return BadRequest(new { code = result.Error.Code, message = result.Error.Description });
        }

        return Ok(new { message = "Blog post updated successfully." });
    }

    public record PublishPostRequest(bool Publish);

    [HttpPatch("posts/{id:guid}/publish")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PublishPost(Guid id, [FromBody] PublishPostRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new PublishBlogPostCommand(id, request.Publish), ct);
        if (result.IsFailure)
        {
            return BadRequest(new { code = result.Error.Code, message = result.Error.Description });
        }

        return Ok(new { message = $"Blog post {(request.Publish ? "published" : "unpublished")} successfully." });
    }

    [HttpDelete("posts/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeletePost(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new DeleteBlogPostCommand(id), ct);
        if (result.IsFailure)
        {
            return BadRequest(new { code = result.Error.Code, message = result.Error.Description });
        }

        return Ok(new { message = "Blog post deleted successfully." });
    }

    public record AddCommentRequest(string UserName, string UserEmail, string Content);

    [HttpPost("posts/{postId:guid}/comments")]
    public async Task<IActionResult> AddComment(Guid postId, [FromBody] AddCommentRequest request, CancellationToken ct)
    {
        Guid? userId = null;
        if (User.Identity?.IsAuthenticated == true && Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid))
        {
            userId = uid;
        }

        var command = new AddBlogCommentCommand(postId, userId, request.UserName, request.UserEmail, request.Content);
        var result = await _sender.Send(command, ct);
        if (result.IsFailure)
        {
            return BadRequest(new { code = result.Error.Code, message = result.Error.Description });
        }

        return Ok(new { commentId = result.Value, message = "Comment submitted successfully. It will appear once approved by moderation." });
    }

    [HttpPatch("posts/{postId:guid}/comments/{commentId:guid}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ApproveComment(Guid postId, Guid commentId, CancellationToken ct)
    {
        var result = await _sender.Send(new ApproveBlogCommentCommand(postId, commentId), ct);
        if (result.IsFailure)
        {
            return BadRequest(new { code = result.Error.Code, message = result.Error.Description });
        }

        return Ok(new { message = "Comment approved successfully." });
    }

    public record CreateBlogCategoryRequest(string Name, string Slug, string? Description);

    [HttpPost("categories")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateBlogCategoryRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new CreateBlogCategoryCommand(request.Name, request.Slug, request.Description), ct);
        if (result.IsFailure)
        {
            return BadRequest(new { code = result.Error.Code, message = result.Error.Description });
        }

        return Ok(new { categoryId = result.Value, message = "Blog category created successfully." });
    }
}
