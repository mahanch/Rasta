using MediatR;
using Shop.Application.Common.Interfaces;
using Shop.Application.Contracts;
using Shop.Domain.Common;
using Shop.Domain.Entities;
using Shop.Domain.Repositories;
using Shop.Domain.ValueObjects;

namespace Shop.Application.Features.Catalog.Commands;

// Category Commands
public record CreateCategoryCommand(
    string Name,
    string Slug,
    string? Description,
    string? ImageUrl,
    Guid? ParentCategoryId
) : IRequest<Result<Guid>>;

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Result<Guid>>
{
    private readonly ICategoryRepository _categoryRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;

    public CreateCategoryCommandHandler(
        ICategoryRepository categoryRepo,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher)
    {
        _categoryRepo = categoryRepo;
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
    }

    public async Task<Result<Guid>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var existing = await _categoryRepo.GetBySlugAsync(request.Slug, cancellationToken);
        if (existing != null)
        {
            return Result<Guid>.Failure(new Error("Category.SlugExists", "A category with this slug already exists."));
        }

        var category = new Category(request.Name, request.Slug, request.Description, request.ImageUrl, request.ParentCategoryId);
        await _categoryRepo.AddAsync(category, cancellationToken);
        await _unitOfWork.CommitChangesAsync(cancellationToken);

        await _eventPublisher.PublishAsync(new CategoryCreatedOrUpdatedIntegrationEvent(
            category.Id,
            category.Name,
            category.Slug,
            category.Description,
            category.ImageUrl,
            category.IsActive,
            category.ParentCategoryId,
            DateTimeOffset.UtcNow
        ), cancellationToken);

        return Result<Guid>.Success(category.Id);
    }
}

// Product Commands
public record CreateProductCommand(
    string Name,
    string Slug,
    string Description,
    string Sku,
    decimal Price,
    decimal? DiscountPrice,
    int InitialStock,
    Guid CategoryId,
    Guid? BrandId,
    List<string>? ImageUrls
) : IRequest<Result<Guid>>;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    private readonly IProductRepository _productRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;

    public CreateProductCommandHandler(
        IProductRepository productRepo,
        ICategoryRepository categoryRepo,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher)
    {
        _productRepo = productRepo;
        _categoryRepo = categoryRepo;
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
    }

    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var existingSku = await _productRepo.GetBySkuAsync(request.Sku, cancellationToken);
        if (existingSku != null)
        {
            return Result<Guid>.Failure(new Error("Product.SkuExists", "A product with this SKU already exists."));
        }

        var category = await _categoryRepo.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category == null)
        {
            return Result<Guid>.Failure(new Error("Product.CategoryNotFound", "The specified category was not found."));
        }

        var skuVo = new Sku(request.Sku);
        var priceVo = new Money(request.Price);
        var discountVo = request.DiscountPrice.HasValue ? new Money(request.DiscountPrice.Value) : null;

        var product = new Product(
            request.Name,
            request.Slug,
            request.Description,
            skuVo,
            priceVo,
            discountVo,
            request.InitialStock,
            category.Id,
            category.Name,
            request.BrandId,
            null,
            request.ImageUrls
        );

        await _productRepo.AddAsync(product, cancellationToken);
        await _unitOfWork.CommitChangesAsync(cancellationToken);

        await _eventPublisher.PublishAsync(new ProductCreatedIntegrationEvent(
            product.Id,
            product.Name,
            product.Slug,
            product.Sku.Value,
            product.Price.Amount,
            product.DiscountPrice?.Amount,
            product.StockQuantity,
            product.CategoryId,
            category.Name,
            product.BrandId,
            null,
            product.Images.Select(i => i.ImageUrl).ToList(),
            product.CreatedAt
        ), cancellationToken);

        return Result<Guid>.Success(product.Id);
    }
}

public record UpdateProductStockCommand(Guid ProductId, int NewStock) : IRequest<Result>;

public class UpdateProductStockCommandHandler : IRequestHandler<UpdateProductStockCommand, Result>
{
    private readonly IProductRepository _productRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;

    public UpdateProductStockCommandHandler(IProductRepository productRepo, IUnitOfWork unitOfWork, IEventPublisher eventPublisher)
    {
        _productRepo = productRepo;
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
    }

    public async Task<Result> Handle(UpdateProductStockCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepo.GetByIdAsync(request.ProductId, cancellationToken);
        if (product == null)
        {
            return Result.Failure(new Error("Product.NotFound", "Product not found."));
        }

        var oldStock = product.StockQuantity;
        product.SetStock(request.NewStock);
        await _unitOfWork.CommitChangesAsync(cancellationToken);

        await _eventPublisher.PublishAsync(new ProductStockChangedIntegrationEvent(
            product.Id,
            oldStock,
            product.StockQuantity,
            DateTimeOffset.UtcNow
        ), cancellationToken);

        return Result.Success();
    }
}
