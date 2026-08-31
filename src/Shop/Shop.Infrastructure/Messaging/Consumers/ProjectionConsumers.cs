using MassTransit;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Shop.Application.Common.Interfaces;
using Shop.Application.Contracts;
using Shop.Application.ReadModels;

namespace Shop.Infrastructure.Messaging.Consumers;

public class ProductProjectionConsumer :
    IConsumer<ProductCreatedIntegrationEvent>,
    IConsumer<ProductUpdatedIntegrationEvent>,
    IConsumer<ProductStockChangedIntegrationEvent>
{
    private readonly IMongoReadDbContext _mongo;
    private readonly ILogger<ProductProjectionConsumer> _logger;

    public ProductProjectionConsumer(IMongoReadDbContext mongo, ILogger<ProductProjectionConsumer> logger)
    {
        _mongo = mongo;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProductCreatedIntegrationEvent> context)
    {
        var msg = context.Message;
        var collection = _mongo.GetCollection<ProductReadModel>("products_view");

        var doc = new ProductReadModel
        {
            Id = msg.ProductId,
            Name = msg.Name,
            Slug = msg.Slug,
            Sku = msg.Sku,
            Price = msg.Price,
            DiscountPrice = msg.DiscountPrice,
            StockQuantity = msg.StockQuantity,
            IsActive = true,
            CategoryId = msg.CategoryId,
            CategoryName = msg.CategoryName,
            BrandId = msg.BrandId,
            BrandName = msg.BrandName,
            ImageUrls = msg.ImageUrls,
            CreatedAt = msg.CreatedAt
        };

        await collection.ReplaceOneAsync(
            p => p.Id == doc.Id,
            doc,
            new ReplaceOptions { IsUpsert = true },
            context.CancellationToken);

        _logger.LogInformation("Product projection created in MongoDB for ProductId: {ProductId}", msg.ProductId);
    }

    public async Task Consume(ConsumeContext<ProductUpdatedIntegrationEvent> context)
    {
        var msg = context.Message;
        var collection = _mongo.GetCollection<ProductReadModel>("products_view");

        var update = Builders<ProductReadModel>.Update
            .Set(p => p.Name, msg.Name)
            .Set(p => p.Slug, msg.Slug)
            .Set(p => p.Sku, msg.Sku)
            .Set(p => p.Price, msg.Price)
            .Set(p => p.DiscountPrice, msg.DiscountPrice)
            .Set(p => p.StockQuantity, msg.StockQuantity)
            .Set(p => p.CategoryId, msg.CategoryId)
            .Set(p => p.CategoryName, msg.CategoryName)
            .Set(p => p.BrandId, msg.BrandId)
            .Set(p => p.BrandName, msg.BrandName)
            .Set(p => p.IsActive, msg.IsActive)
            .Set(p => p.ImageUrls, msg.ImageUrls)
            .Set(p => p.UpdatedAt, msg.UpdatedAt);

        await collection.UpdateOneAsync(p => p.Id == msg.ProductId, update, cancellationToken: context.CancellationToken);
        _logger.LogInformation("Product projection updated in MongoDB for ProductId: {ProductId}", msg.ProductId);
    }

    public async Task Consume(ConsumeContext<ProductStockChangedIntegrationEvent> context)
    {
        var msg = context.Message;
        var collection = _mongo.GetCollection<ProductReadModel>("products_view");

        var update = Builders<ProductReadModel>.Update
            .Set(p => p.StockQuantity, msg.NewStock);

        await collection.UpdateOneAsync(p => p.Id == msg.ProductId, update, cancellationToken: context.CancellationToken);
        _logger.LogInformation("Product stock updated in MongoDB for ProductId: {ProductId} -> {NewStock}", msg.ProductId, msg.NewStock);
    }
}

public class CategoryProjectionConsumer : IConsumer<CategoryCreatedOrUpdatedIntegrationEvent>
{
    private readonly IMongoReadDbContext _mongo;
    private readonly ILogger<CategoryProjectionConsumer> _logger;

    public CategoryProjectionConsumer(IMongoReadDbContext mongo, ILogger<CategoryProjectionConsumer> logger)
    {
        _mongo = mongo;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CategoryCreatedOrUpdatedIntegrationEvent> context)
    {
        var msg = context.Message;
        var collection = _mongo.GetCollection<CategoryReadModel>("categories_view");

        var doc = new CategoryReadModel
        {
            Id = msg.CategoryId,
            Name = msg.Name,
            Slug = msg.Slug,
            Description = msg.Description,
            ImageUrl = msg.ImageUrl,
            IsActive = msg.IsActive,
            ParentCategoryId = msg.ParentCategoryId,
            CreatedAt = msg.Timestamp
        };

        await collection.ReplaceOneAsync(
            c => c.Id == doc.Id,
            doc,
            new ReplaceOptions { IsUpsert = true },
            context.CancellationToken);

        _logger.LogInformation("Category projection updated in MongoDB for CategoryId: {CategoryId}", msg.CategoryId);
    }
}

public class OrderProjectionConsumer :
    IConsumer<OrderCreatedIntegrationEvent>,
    IConsumer<OrderPaidIntegrationEvent>,
    IConsumer<OrderStatusChangedIntegrationEvent>,
    IConsumer<OrderCancelledIntegrationEvent>
{
    private readonly IMongoReadDbContext _mongo;
    private readonly ILogger<OrderProjectionConsumer> _logger;

    public OrderProjectionConsumer(IMongoReadDbContext mongo, ILogger<OrderProjectionConsumer> logger)
    {
        _mongo = mongo;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCreatedIntegrationEvent> context)
    {
        var msg = context.Message;
        var collection = _mongo.GetCollection<OrderReadModel>("orders_view");

        var doc = new OrderReadModel
        {
            Id = msg.OrderId,
            OrderNumber = msg.OrderNumber,
            UserId = msg.UserId,
            Status = msg.Status,
            PaymentStatus = msg.PaymentStatus,
            TotalAmount = msg.TotalAmount,
            DiscountAmount = msg.DiscountAmount,
            FinalAmount = msg.FinalAmount,
            ShippingAddress = new OrderAddressReadModel
            {
                Street = msg.Street,
                City = msg.City,
                State = msg.State,
                PostalCode = msg.PostalCode,
                Country = msg.Country,
                RecipientName = msg.RecipientName,
                PhoneNumber = msg.PhoneNumber
            },
            Items = msg.Items.Select(i => new OrderItemReadModel
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Sku = i.Sku,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity,
                TotalPrice = i.TotalPrice,
                ImageUrl = i.ImageUrl
            }).ToList(),
            CreatedAt = msg.CreatedAt
        };

        await collection.ReplaceOneAsync(
            o => o.Id == doc.Id,
            doc,
            new ReplaceOptions { IsUpsert = true },
            context.CancellationToken);

        _logger.LogInformation("Order projection created in MongoDB for Order: {OrderNumber}", msg.OrderNumber);
    }

    public async Task Consume(ConsumeContext<OrderPaidIntegrationEvent> context)
    {
        var msg = context.Message;
        var collection = _mongo.GetCollection<OrderReadModel>("orders_view");

        var update = Builders<OrderReadModel>.Update
            .Set(o => o.Status, "Paid")
            .Set(o => o.PaymentStatus, "Success")
            .Set(o => o.PaymentTransactionId, msg.TransactionReference)
            .Set(o => o.PaidAt, msg.PaidAt);

        await collection.UpdateOneAsync(o => o.Id == msg.OrderId, update, cancellationToken: context.CancellationToken);
        _logger.LogInformation("Order projection marked as Paid in MongoDB for Order: {OrderNumber}", msg.OrderNumber);
    }

    public async Task Consume(ConsumeContext<OrderStatusChangedIntegrationEvent> context)
    {
        var msg = context.Message;
        var collection = _mongo.GetCollection<OrderReadModel>("orders_view");

        var update = Builders<OrderReadModel>.Update.Set(o => o.Status, msg.NewStatus);
        await collection.UpdateOneAsync(o => o.Id == msg.OrderId, update, cancellationToken: context.CancellationToken);
        _logger.LogInformation("Order projection status updated in MongoDB for Order {OrderNumber} -> {NewStatus}", msg.OrderNumber, msg.NewStatus);
    }

    public async Task Consume(ConsumeContext<OrderCancelledIntegrationEvent> context)
    {
        var msg = context.Message;
        var collection = _mongo.GetCollection<OrderReadModel>("orders_view");

        var update = Builders<OrderReadModel>.Update
            .Set(o => o.Status, "Cancelled")
            .Set(o => o.CancellationReason, msg.Reason);

        await collection.UpdateOneAsync(o => o.Id == msg.OrderId, update, cancellationToken: context.CancellationToken);
        _logger.LogInformation("Order projection cancelled in MongoDB for Order {OrderNumber}", msg.OrderNumber);
    }
}

public class BlogPostProjectionConsumer :
    IConsumer<BlogPostCreatedIntegrationEvent>,
    IConsumer<BlogPostUpdatedIntegrationEvent>,
    IConsumer<BlogPostDeletedIntegrationEvent>,
    IConsumer<BlogCommentAddedIntegrationEvent>,
    IConsumer<BlogCommentApprovedIntegrationEvent>
{
    private readonly IMongoReadDbContext _mongo;
    private readonly ILogger<BlogPostProjectionConsumer> _logger;

    public BlogPostProjectionConsumer(IMongoReadDbContext mongo, ILogger<BlogPostProjectionConsumer> logger)
    {
        _mongo = mongo;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<BlogPostCreatedIntegrationEvent> context)
    {
        var msg = context.Message;
        var collection = _mongo.GetCollection<BlogPostReadModel>("blog_posts_view");

        var doc = new BlogPostReadModel
        {
            Id = msg.PostId,
            Title = msg.Title,
            Slug = msg.Slug,
            Summary = msg.Summary,
            Content = msg.Content,
            CoverImageUrl = msg.CoverImageUrl,
            AuthorId = msg.AuthorId,
            AuthorName = msg.AuthorName,
            CategoryId = msg.CategoryId,
            CategoryName = msg.CategoryName,
            Tags = msg.Tags,
            ReadingTimeMinutes = msg.ReadingTimeMinutes,
            IsPublished = msg.IsPublished,
            PublishedAt = msg.PublishedAt,
            CreatedAt = msg.CreatedAt
        };

        await collection.ReplaceOneAsync(
            p => p.Id == doc.Id,
            doc,
            new ReplaceOptions { IsUpsert = true },
            context.CancellationToken);

        _logger.LogInformation("BlogPost projection created in MongoDB for PostId: {PostId}", msg.PostId);
    }

    public async Task Consume(ConsumeContext<BlogPostUpdatedIntegrationEvent> context)
    {
        var msg = context.Message;
        var collection = _mongo.GetCollection<BlogPostReadModel>("blog_posts_view");

        var update = Builders<BlogPostReadModel>.Update
            .Set(p => p.Title, msg.Title)
            .Set(p => p.Slug, msg.Slug)
            .Set(p => p.Summary, msg.Summary)
            .Set(p => p.Content, msg.Content)
            .Set(p => p.CoverImageUrl, msg.CoverImageUrl)
            .Set(p => p.CategoryId, msg.CategoryId)
            .Set(p => p.CategoryName, msg.CategoryName)
            .Set(p => p.Tags, msg.Tags)
            .Set(p => p.ReadingTimeMinutes, msg.ReadingTimeMinutes)
            .Set(p => p.IsPublished, msg.IsPublished)
            .Set(p => p.PublishedAt, msg.PublishedAt)
            .Set(p => p.UpdatedAt, msg.UpdatedAt);

        await collection.UpdateOneAsync(p => p.Id == msg.PostId, update, cancellationToken: context.CancellationToken);
        _logger.LogInformation("BlogPost projection updated in MongoDB for PostId: {PostId}", msg.PostId);
    }

    public async Task Consume(ConsumeContext<BlogPostDeletedIntegrationEvent> context)
    {
        var msg = context.Message;
        var collection = _mongo.GetCollection<BlogPostReadModel>("blog_posts_view");
        await collection.DeleteOneAsync(p => p.Id == msg.PostId, context.CancellationToken);
        _logger.LogInformation("BlogPost projection deleted in MongoDB for PostId: {PostId}", msg.PostId);
    }

    public async Task Consume(ConsumeContext<BlogCommentAddedIntegrationEvent> context)
    {
        var msg = context.Message;
        var collection = _mongo.GetCollection<BlogPostReadModel>("blog_posts_view");

        var comment = new BlogCommentReadModel
        {
            Id = msg.CommentId,
            UserId = msg.UserId,
            UserName = msg.UserName,
            Content = msg.Content,
            IsApproved = msg.IsApproved,
            CreatedAt = msg.CreatedAt
        };

        var update = Builders<BlogPostReadModel>.Update.Push(p => p.Comments, comment);
        await collection.UpdateOneAsync(p => p.Id == msg.PostId, update, cancellationToken: context.CancellationToken);
        _logger.LogInformation("BlogComment added in MongoDB for PostId: {PostId}", msg.PostId);
    }

    public async Task Consume(ConsumeContext<BlogCommentApprovedIntegrationEvent> context)
    {
        var msg = context.Message;
        var collection = _mongo.GetCollection<BlogPostReadModel>("blog_posts_view");

        var filter = Builders<BlogPostReadModel>.Filter.And(
            Builders<BlogPostReadModel>.Filter.Eq(p => p.Id, msg.PostId),
            Builders<BlogPostReadModel>.Filter.ElemMatch(p => p.Comments, c => c.Id == msg.CommentId)
        );

        var update = Builders<BlogPostReadModel>.Update.Set("Comments.$.IsApproved", true);
        await collection.UpdateOneAsync(filter, update, cancellationToken: context.CancellationToken);
        _logger.LogInformation("BlogComment approved in MongoDB for PostId: {PostId}, CommentId: {CommentId}", msg.PostId, msg.CommentId);
    }
}

public class BlogCategoryProjectionConsumer : IConsumer<BlogCategoryCreatedIntegrationEvent>
{
    private readonly IMongoReadDbContext _mongo;
    private readonly ILogger<BlogCategoryProjectionConsumer> _logger;

    public BlogCategoryProjectionConsumer(IMongoReadDbContext mongo, ILogger<BlogCategoryProjectionConsumer> logger)
    {
        _mongo = mongo;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<BlogCategoryCreatedIntegrationEvent> context)
    {
        var msg = context.Message;
        var collection = _mongo.GetCollection<BlogCategoryReadModel>("blog_categories_view");

        var doc = new BlogCategoryReadModel
        {
            Id = msg.CategoryId,
            Name = msg.Name,
            Slug = msg.Slug,
            Description = msg.Description,
            CreatedAt = msg.CreatedAt
        };

        await collection.ReplaceOneAsync(
            c => c.Id == doc.Id,
            doc,
            new ReplaceOptions { IsUpsert = true },
            context.CancellationToken);

        _logger.LogInformation("BlogCategory projection created in MongoDB for CategoryId: {CategoryId}", msg.CategoryId);
    }
}
