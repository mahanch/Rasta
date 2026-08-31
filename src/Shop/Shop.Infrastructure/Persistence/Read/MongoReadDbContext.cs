using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Shop.Application.Common.Interfaces;
using Shop.Application.ReadModels;

namespace Shop.Infrastructure.Persistence.Read;

public class MongoReadDbContext : IMongoReadDbContext
{
    private readonly IMongoDatabase _database;

    public MongoReadDbContext(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("shop-read-db")
            ?? configuration.GetConnectionString("MongoConnection")
            ?? "mongodb://localhost:27017";

        var mongoUrl = new MongoUrl(connectionString);
        var client = new MongoClient(mongoUrl);
        var databaseName = mongoUrl.DatabaseName ?? "shop_read_db";
        _database = client.GetDatabase(databaseName);

        InitIndexes();
    }

    public IMongoCollection<T> GetCollection<T>(string collectionName)
    {
        return _database.GetCollection<T>(collectionName);
    }

    private void InitIndexes()
    {
        try
        {
            var productCollection = _database.GetCollection<ProductReadModel>("products_view");
            var productIndexBuilder = Builders<ProductReadModel>.IndexKeys;
            productCollection.Indexes.CreateMany([
                new CreateIndexModel<ProductReadModel>(productIndexBuilder.Ascending(p => p.Slug), new CreateIndexOptions { Unique = true }),
                new CreateIndexModel<ProductReadModel>(productIndexBuilder.Ascending(p => p.CategoryId)),
                new CreateIndexModel<ProductReadModel>(productIndexBuilder.Ascending(p => p.Price))
            ]);

            var categoryCollection = _database.GetCollection<CategoryReadModel>("categories_view");
            var categoryIndexBuilder = Builders<CategoryReadModel>.IndexKeys;
            categoryCollection.Indexes.CreateOne(
                new CreateIndexModel<CategoryReadModel>(categoryIndexBuilder.Ascending(c => c.Slug), new CreateIndexOptions { Unique = true })
            );

            var orderCollection = _database.GetCollection<OrderReadModel>("orders_view");
            var orderIndexBuilder = Builders<OrderReadModel>.IndexKeys;
            orderCollection.Indexes.CreateMany([
                new CreateIndexModel<OrderReadModel>(orderIndexBuilder.Ascending(o => o.OrderNumber), new CreateIndexOptions { Unique = true }),
                new CreateIndexModel<OrderReadModel>(orderIndexBuilder.Ascending(o => o.UserId))
            ]);

            var blogCollection = _database.GetCollection<BlogPostReadModel>("blog_posts_view");
            var blogIndexBuilder = Builders<BlogPostReadModel>.IndexKeys;
            blogCollection.Indexes.CreateMany([
                new CreateIndexModel<BlogPostReadModel>(blogIndexBuilder.Ascending(b => b.Slug), new CreateIndexOptions { Unique = true }),
                new CreateIndexModel<BlogPostReadModel>(blogIndexBuilder.Ascending(b => b.CategoryId)),
                new CreateIndexModel<BlogPostReadModel>(blogIndexBuilder.Descending(b => b.PublishedAt))
            ]);

            var blogCategoryCollection = _database.GetCollection<BlogCategoryReadModel>("blog_categories_view");
            var blogCatIndexBuilder = Builders<BlogCategoryReadModel>.IndexKeys;
            blogCategoryCollection.Indexes.CreateOne(
                new CreateIndexModel<BlogCategoryReadModel>(blogCatIndexBuilder.Ascending(c => c.Slug), new CreateIndexOptions { Unique = true })
            );
        }
        catch
        {
            // Silently handle if server is not yet connected during configuration
        }
    }
}
