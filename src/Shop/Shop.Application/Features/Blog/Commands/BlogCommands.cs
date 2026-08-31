using MediatR;
using Shop.Application.Common.Interfaces;
using Shop.Application.Contracts;
using Shop.Domain.Common;
using Shop.Domain.Entities;
using Shop.Domain.Repositories;

namespace Shop.Application.Features.Blog.Commands;

public record CreateBlogPostCommand(
    string Title,
    string Slug,
    string Summary,
    string Content,
    Guid AuthorId,
    string AuthorName,
    Guid? CategoryId,
    string? CoverImageUrl,
    List<string>? Tags,
    bool PublishImmediately = true
) : IRequest<Result<Guid>>;

public class CreateBlogPostCommandHandler : IRequestHandler<CreateBlogPostCommand, Result<Guid>>
{
    private readonly IBlogRepository _blogRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;

    public CreateBlogPostCommandHandler(IBlogRepository blogRepo, IUnitOfWork unitOfWork, IEventPublisher eventPublisher)
    {
        _blogRepo = blogRepo;
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
    }

    public async Task<Result<Guid>> Handle(CreateBlogPostCommand request, CancellationToken cancellationToken)
    {
        var existing = await _blogRepo.GetBySlugAsync(request.Slug, cancellationToken);
        if (existing != null)
        {
            return Result<Guid>.Failure(new Error("Blog.SlugExists", "A blog post with this slug already exists."));
        }

        string? categoryName = null;
        if (request.CategoryId.HasValue)
        {
            var category = await _blogRepo.GetCategoryByIdAsync(request.CategoryId.Value, cancellationToken);
            categoryName = category?.Name;
        }

        var post = BlogPost.Create(
            request.Title,
            request.Slug,
            request.Summary,
            request.Content,
            request.AuthorId,
            request.AuthorName,
            request.CategoryId,
            categoryName,
            request.CoverImageUrl,
            request.Tags,
            request.PublishImmediately
        );

        await _blogRepo.AddPostAsync(post, cancellationToken);
        await _unitOfWork.CommitChangesAsync(cancellationToken);

        await _eventPublisher.PublishAsync(new BlogPostCreatedIntegrationEvent(
            post.Id,
            post.Title,
            post.Slug,
            post.Summary,
            post.Content,
            post.CoverImageUrl,
            post.AuthorId,
            post.AuthorName,
            post.CategoryId,
            categoryName,
            post.Tags,
            post.ReadingTimeMinutes,
            post.IsPublished,
            post.PublishedAt,
            post.CreatedAt
        ), cancellationToken);

        return Result<Guid>.Success(post.Id);
    }
}

public record UpdateBlogPostCommand(
    Guid Id,
    string Title,
    string Slug,
    string Summary,
    string Content,
    Guid? CategoryId,
    string? CoverImageUrl,
    List<string>? Tags,
    bool IsPublished
) : IRequest<Result>;

public class UpdateBlogPostCommandHandler : IRequestHandler<UpdateBlogPostCommand, Result>
{
    private readonly IBlogRepository _blogRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;

    public UpdateBlogPostCommandHandler(IBlogRepository blogRepo, IUnitOfWork unitOfWork, IEventPublisher eventPublisher)
    {
        _blogRepo = blogRepo;
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
    }

    public async Task<Result> Handle(UpdateBlogPostCommand request, CancellationToken cancellationToken)
    {
        var post = await _blogRepo.GetByIdAsync(request.Id, cancellationToken);
        if (post == null) return Result.Failure(new Error("Blog.NotFound", "Blog post not found."));

        string? categoryName = null;
        if (request.CategoryId.HasValue)
        {
            var category = await _blogRepo.GetCategoryByIdAsync(request.CategoryId.Value, cancellationToken);
            categoryName = category?.Name;
        }

        post.Update(
            request.Title,
            request.Slug,
            request.Summary,
            request.Content,
            request.CategoryId,
            categoryName,
            request.CoverImageUrl,
            request.Tags,
            request.IsPublished
        );

        await _unitOfWork.CommitChangesAsync(cancellationToken);

        await _eventPublisher.PublishAsync(new BlogPostUpdatedIntegrationEvent(
            post.Id,
            post.Title,
            post.Slug,
            post.Summary,
            post.Content,
            post.CoverImageUrl,
            post.CategoryId,
            categoryName,
            post.Tags,
            post.ReadingTimeMinutes,
            post.IsPublished,
            post.PublishedAt,
            post.UpdatedAt ?? DateTimeOffset.UtcNow
        ), cancellationToken);

        return Result.Success();
    }
}

public record PublishBlogPostCommand(Guid Id, bool Publish) : IRequest<Result>;

public class PublishBlogPostCommandHandler : IRequestHandler<PublishBlogPostCommand, Result>
{
    private readonly IBlogRepository _blogRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;

    public PublishBlogPostCommandHandler(IBlogRepository blogRepo, IUnitOfWork unitOfWork, IEventPublisher eventPublisher)
    {
        _blogRepo = blogRepo;
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
    }

    public async Task<Result> Handle(PublishBlogPostCommand request, CancellationToken cancellationToken)
    {
        var post = await _blogRepo.GetByIdAsync(request.Id, cancellationToken);
        if (post == null) return Result.Failure(new Error("Blog.NotFound", "Blog post not found."));

        if (request.Publish) post.Publish();
        else post.Unpublish();

        await _unitOfWork.CommitChangesAsync(cancellationToken);

        await _eventPublisher.PublishAsync(new BlogPostUpdatedIntegrationEvent(
            post.Id,
            post.Title,
            post.Slug,
            post.Summary,
            post.Content,
            post.CoverImageUrl,
            post.CategoryId,
            post.Category?.Name,
            post.Tags,
            post.ReadingTimeMinutes,
            post.IsPublished,
            post.PublishedAt,
            post.UpdatedAt ?? DateTimeOffset.UtcNow
        ), cancellationToken);

        return Result.Success();
    }
}

public record DeleteBlogPostCommand(Guid Id) : IRequest<Result>;

public class DeleteBlogPostCommandHandler : IRequestHandler<DeleteBlogPostCommand, Result>
{
    private readonly IBlogRepository _blogRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;

    public DeleteBlogPostCommandHandler(IBlogRepository blogRepo, IUnitOfWork unitOfWork, IEventPublisher eventPublisher)
    {
        _blogRepo = blogRepo;
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
    }

    public async Task<Result> Handle(DeleteBlogPostCommand request, CancellationToken cancellationToken)
    {
        var post = await _blogRepo.GetByIdAsync(request.Id, cancellationToken);
        if (post == null) return Result.Failure(new Error("Blog.NotFound", "Blog post not found."));

        _blogRepo.DeletePost(post);
        await _unitOfWork.CommitChangesAsync(cancellationToken);

        await _eventPublisher.PublishAsync(new BlogPostDeletedIntegrationEvent(post.Id), cancellationToken);
        return Result.Success();
    }
}

public record AddBlogCommentCommand(
    Guid PostId,
    Guid? UserId,
    string UserName,
    string UserEmail,
    string Content
) : IRequest<Result<Guid>>;

public class AddBlogCommentCommandHandler : IRequestHandler<AddBlogCommentCommand, Result<Guid>>
{
    private readonly IBlogRepository _blogRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;

    public AddBlogCommentCommandHandler(IBlogRepository blogRepo, IUnitOfWork unitOfWork, IEventPublisher eventPublisher)
    {
        _blogRepo = blogRepo;
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
    }

    public async Task<Result<Guid>> Handle(AddBlogCommentCommand request, CancellationToken cancellationToken)
    {
        var post = await _blogRepo.GetByIdAsync(request.PostId, cancellationToken);
        if (post == null) return Result<Guid>.Failure(new Error("Blog.NotFound", "Blog post not found."));

        // Auto approve comments or keep pending for moderation
        var comment = post.AddComment(request.UserId, request.UserName, request.UserEmail, request.Content, autoApprove: false);

        await _unitOfWork.CommitChangesAsync(cancellationToken);

        await _eventPublisher.PublishAsync(new BlogCommentAddedIntegrationEvent(
            comment.Id,
            post.Id,
            request.UserId,
            request.UserName,
            request.Content,
            comment.IsApproved,
            comment.CreatedAt
        ), cancellationToken);

        return Result<Guid>.Success(comment.Id);
    }
}

public record ApproveBlogCommentCommand(Guid PostId, Guid CommentId) : IRequest<Result>;

public class ApproveBlogCommentCommandHandler : IRequestHandler<ApproveBlogCommentCommand, Result>
{
    private readonly IBlogRepository _blogRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;

    public ApproveBlogCommentCommandHandler(IBlogRepository blogRepo, IUnitOfWork unitOfWork, IEventPublisher eventPublisher)
    {
        _blogRepo = blogRepo;
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
    }

    public async Task<Result> Handle(ApproveBlogCommentCommand request, CancellationToken cancellationToken)
    {
        var post = await _blogRepo.GetByIdAsync(request.PostId, cancellationToken);
        if (post == null) return Result.Failure(new Error("Blog.NotFound", "Blog post not found."));

        post.ApproveComment(request.CommentId);
        await _unitOfWork.CommitChangesAsync(cancellationToken);

        await _eventPublisher.PublishAsync(new BlogCommentApprovedIntegrationEvent(post.Id, request.CommentId), cancellationToken);

        return Result.Success();
    }
}

public record CreateBlogCategoryCommand(string Name, string Slug, string? Description = null) : IRequest<Result<Guid>>;

public class CreateBlogCategoryCommandHandler : IRequestHandler<CreateBlogCategoryCommand, Result<Guid>>
{
    private readonly IBlogRepository _blogRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;

    public CreateBlogCategoryCommandHandler(IBlogRepository blogRepo, IUnitOfWork unitOfWork, IEventPublisher eventPublisher)
    {
        _blogRepo = blogRepo;
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
    }

    public async Task<Result<Guid>> Handle(CreateBlogCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = new BlogCategory(request.Name, request.Slug, request.Description);
        await _blogRepo.AddCategoryAsync(category, cancellationToken);
        await _unitOfWork.CommitChangesAsync(cancellationToken);

        await _eventPublisher.PublishAsync(new BlogCategoryCreatedIntegrationEvent(
            category.Id,
            category.Name,
            category.Slug,
            category.Description,
            category.CreatedAt
        ), cancellationToken);

        return Result<Guid>.Success(category.Id);
    }
}
