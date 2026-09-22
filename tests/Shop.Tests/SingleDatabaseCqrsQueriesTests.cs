using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Features.Blog.Queries;
using Shop.Application.Features.Catalog.Queries;
using Shop.Application.Features.Orders.Queries;
using Shop.Domain.Entities;
using Shop.Domain.ValueObjects;
using Shop.Infrastructure.Persistence;
using Xunit;

namespace Shop.Tests.Application;

public class SingleDatabaseCqrsQueriesTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ShopDbContext _db;

    public SingleDatabaseCqrsQueriesTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ShopDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new ShopDbContext(options);
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task GetProductsQuery_ShouldReturnFilteredAndSortedProductsDirectlyFromDatabase()
    {
        // Arrange
        var category = new Category("Electronics", "electronics", "Desc");
        _db.Categories.Add(category);

        var p1 = new Product("Alpha Laptop", "alpha-laptop", "Great laptop", new Sku("SKU-1"), new Money(1000m), null, 5, category.Id, category.Name);
        var p2 = new Product("Beta Phone", "beta-phone", "Smartphone", new Sku("SKU-2"), new Money(500m), null, 10, category.Id, category.Name);
        var p3 = new Product("Gamma Tablet", "gamma-tablet", "Tablet device", new Sku("SKU-3"), new Money(800m), null, 0, category.Id, category.Name);

        _db.Products.AddRange(p1, p2, p3);
        await _db.SaveChangesAsync();

        var handler = new GetProductsQueryHandler(_db);

        // Act 1: Filter by search
        var searchResult = await handler.Handle(new GetProductsQuery(Search: "Laptop"), CancellationToken.None);
        searchResult.Items.Should().HaveCount(1);
        searchResult.Items.First().Name.Should().Be("Alpha Laptop");

        // Act 2: Sort by price ascending
        var sortResult = await handler.Handle(new GetProductsQuery(SortBy: "price_asc"), CancellationToken.None);
        sortResult.Items.Should().HaveCount(3);
        sortResult.Items[0].Price.Should().Be(500m);
        sortResult.Items[1].Price.Should().Be(800m);
        sortResult.Items[2].Price.Should().Be(1000m);

        // Act 3: Filter in-stock only
        var inStockResult = await handler.Handle(new GetProductsQuery(InStockOnly: true), CancellationToken.None);
        inStockResult.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetBlogPostBySlugQuery_ShouldReturnPostAndIncrementViewCountDirectlyInDatabase()
    {
        // Arrange
        var author = new User("admin@shop.local", "Admin User", "hash", "Admin");
        _db.Users.Add(author);
        await _db.SaveChangesAsync();

        var post = BlogPost.Create(
            "CQRS on Single PostgreSQL",
            "cqrs-single-postgres",
            "Summary",
            "Content",
            author.Id,
            author.FullName,
            publishImmediately: true
        );
        post.AddComment(null, "User1", "u1@test.com", "Nice article", autoApprove: true);
        _db.BlogPosts.Add(post);
        await _db.SaveChangesAsync();

        var handler = new GetBlogPostBySlugQueryHandler(_db);

        // Act
        var result = await handler.Handle(new GetBlogPostBySlugQuery("cqrs-single-postgres"), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("CQRS on Single PostgreSQL");
        result.Value.ViewCount.Should().Be(1);
        result.Value.Comments.Should().HaveCount(1);

        // Verify in DB that ViewCount actually persisted
        var postInDb = await _db.BlogPosts.FirstAsync(p => p.Id == post.Id);
        postInDb.ViewCount.Should().Be(1);
    }

    [Fact]
    public async Task GetCustomerOrdersQuery_ShouldReturnOrdersDirectlyFromDatabase()
    {
        // Arrange
        var user1 = new User("user1@shop.local", "User One", "hash", "Customer");
        var user2 = new User("user2@shop.local", "User Two", "hash", "Customer");
        _db.Users.AddRange(user1, user2);
        await _db.SaveChangesAsync();

        var address = new Address("Street 1", "City", "State", "12345", "Country", "Recipient", "123456");
        var items = new List<(Guid productId, string productName, string sku, decimal unitPrice, int quantity, string? imageUrl)>
        {
            (Guid.NewGuid(), "Item 1", "SKU-1", 100m, 2, "img.jpg")
        };

        var order1 = Order.Create(user1.Id, address, items);
        var order2 = Order.Create(user2.Id, address, items);

        _db.Orders.AddRange(order1, order2);
        await _db.SaveChangesAsync();

        var handler = new GetCustomerOrdersQueryHandler(_db);

        // Act
        var result = await handler.Handle(new GetCustomerOrdersQuery(user1.Id), CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().UserId.Should().Be(user1.Id);
    }
}
