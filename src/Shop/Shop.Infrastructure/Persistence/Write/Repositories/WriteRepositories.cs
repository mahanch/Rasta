using Microsoft.EntityFrameworkCore;
using Shop.Domain.Entities;
using Shop.Domain.Repositories;
using Shop.Infrastructure.Persistence;

namespace Shop.Infrastructure.Persistence.Write.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ShopDbContext _db;

    public UnitOfWork(ShopDbContext db)
    {
        _db = db;
    }

    public async Task<int> CommitChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _db.SaveChangesAsync(cancellationToken);
    }
}

public class UserRepository : IUserRepository
{
    private readonly ShopDbContext _db;

    public UserRepository(ShopDbContext db) => _db = db;

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _db.Users.Include(u => u.RefreshTokens).FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await _db.Users.Include(u => u.RefreshTokens).FirstOrDefaultAsync(u => u.Email == email.Trim().ToLowerInvariant(), cancellationToken);

    public async Task<User?> GetByRefreshTokenAsync(string token, CancellationToken cancellationToken = default) =>
        await _db.Users.Include(u => u.RefreshTokens).FirstOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == token), cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default) =>
        await _db.Users.AddAsync(user, cancellationToken);

    public void Update(User user) => _db.Users.Update(user);
}

public class CategoryRepository : ICategoryRepository
{
    private readonly ShopDbContext _db;

    public CategoryRepository(ShopDbContext db) => _db = db;

    public async Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _db.Categories.Include(c => c.SubCategories).FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        await _db.Categories.FirstOrDefaultAsync(c => c.Slug == slug.ToLowerInvariant(), cancellationToken);

    public async Task<List<Category>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _db.Categories.ToListAsync(cancellationToken);

    public async Task AddAsync(Category category, CancellationToken cancellationToken = default) =>
        await _db.Categories.AddAsync(category, cancellationToken);

    public void Update(Category category) => _db.Categories.Update(category);

    public void Delete(Category category) => _db.Categories.Remove(category);
}

public class ProductRepository : IProductRepository
{
    private readonly ShopDbContext _db;

    public ProductRepository(ShopDbContext db) => _db = db;

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _db.Products.Include(p => p.Category).Include(p => p.Brand).Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        await _db.Products.Include(p => p.Category).Include(p => p.Brand).Include(p => p.Images).FirstOrDefaultAsync(p => p.Slug == slug.ToLowerInvariant(), cancellationToken);

    public async Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default) =>
        await _db.Products.FirstOrDefaultAsync(p => p.Sku.Value == sku.ToUpperInvariant(), cancellationToken);

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default) =>
        await _db.Products.AddAsync(product, cancellationToken);

    public void Update(Product product) => _db.Products.Update(product);

    public void Delete(Product product) => _db.Products.Remove(product);
}

public class CartRepository : ICartRepository
{
    private readonly ShopDbContext _db;

    public CartRepository(ShopDbContext db) => _db = db;

    public async Task<Cart?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await _db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

    public async Task AddAsync(Cart cart, CancellationToken cancellationToken = default) =>
        await _db.Carts.AddAsync(cart, cancellationToken);

    public void Update(Cart cart) => _db.Carts.Update(cart);
}

public class OrderRepository : IOrderRepository
{
    private readonly ShopDbContext _db;

    public OrderRepository(ShopDbContext db) => _db = db;

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _db.Orders.Include(o => o.Items).Include(o => o.User).FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default) =>
        await _db.Orders.Include(o => o.Items).Include(o => o.User).FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, cancellationToken);

    public async Task<int> GetTotalPaidOrderCountAsync(CancellationToken cancellationToken = default) =>
        await _db.Orders.CountAsync(o => o.Status == OrderStatus.Paid, cancellationToken);

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default) =>
        await _db.Orders.AddAsync(order, cancellationToken);

    public void Update(Order order) => _db.Orders.Update(order);
}

public class PaymentRepository : IPaymentRepository
{
    private readonly ShopDbContext _db;

    public PaymentRepository(ShopDbContext db) => _db = db;

    public async Task<PaymentTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _db.PaymentTransactions.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<PaymentTransaction?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        await _db.PaymentTransactions.FirstOrDefaultAsync(p => p.OrderId == orderId, cancellationToken);

    public async Task<PaymentTransaction?> GetByReferenceAsync(string reference, CancellationToken cancellationToken = default) =>
        await _db.PaymentTransactions.FirstOrDefaultAsync(p => p.TransactionReference == reference, cancellationToken);

    public async Task AddAsync(PaymentTransaction transaction, CancellationToken cancellationToken = default) =>
        await _db.PaymentTransactions.AddAsync(transaction, cancellationToken);

    public void Update(PaymentTransaction transaction) => _db.PaymentTransactions.Update(transaction);
}

public class BlogRepository : IBlogRepository
{
    private readonly ShopDbContext _db;

    public BlogRepository(ShopDbContext db) => _db = db;

    public async Task<BlogPost?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _db.BlogPosts.Include(p => p.Category).Include(p => p.Comments).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<BlogPost?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        await _db.BlogPosts.Include(p => p.Category).Include(p => p.Comments).FirstOrDefaultAsync(p => p.Slug == slug.ToLowerInvariant(), cancellationToken);

    public async Task<List<BlogCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        await _db.BlogCategories.ToListAsync(cancellationToken);

    public async Task<BlogCategory?> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _db.BlogCategories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task AddPostAsync(BlogPost post, CancellationToken cancellationToken = default) =>
        await _db.BlogPosts.AddAsync(post, cancellationToken);

    public async Task AddCategoryAsync(BlogCategory category, CancellationToken cancellationToken = default) =>
        await _db.BlogCategories.AddAsync(category, cancellationToken);

    public void UpdatePost(BlogPost post) => _db.BlogPosts.Update(post);

    public void DeletePost(BlogPost post) => _db.BlogPosts.Remove(post);
}

