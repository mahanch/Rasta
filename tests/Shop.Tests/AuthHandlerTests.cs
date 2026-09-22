using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Shop.Domain.Entities;
using Shop.Application.DTOs;
using Shop.Application.Features.Auth.Commands;
using Shop.Infrastructure.Persistence;
using Shop.Infrastructure.Persistence.Write.Repositories;
using Shop.Infrastructure.Security;
using Xunit;

namespace Shop.Tests.Application;

public class AuthHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ShopDbContext _db;
    private readonly UserRepository _userRepo;
    private readonly UnitOfWork _unitOfWork;
    private readonly PasswordHasher _passwordHasher;
    private readonly JwtTokenService _jwtService;

    public AuthHandlerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ShopDbContext>()
            .UseSqlite(_connection)
            .LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information)
            .EnableSensitiveDataLogging()
            .Options;

        _db = new ShopDbContext(options);
        _db.Database.EnsureCreated();

        _userRepo = new UserRepository(_db);
        _unitOfWork = new UnitOfWork(_db);
        _passwordHasher = new PasswordHasher();

        var inMemoryConfig = new Dictionary<string, string?>
        {
            {"JwtSettings:Secret", "THIS_IS_A_VERY_LONG_SECRET_KEY_FOR_TESTING_1234567890"},
            {"JwtSettings:Issuer", "TestIssuer"},
            {"JwtSettings:Audience", "TestAudience"},
            {"JwtSettings:ExpiryMinutes", "60"}
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();
        _jwtService = new JwtTokenService(config);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task RegisterUserCommand_ShouldSucceed()
    {
        var handler = new RegisterUserCommandHandler(_userRepo, _unitOfWork, _passwordHasher, _jwtService);
        var command = new RegisterUserCommand("test@example.com", "Password123!", "Test User", "1234567890");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task LoginUserCommand_ShouldSucceed()
    {
        var regHandler = new RegisterUserCommandHandler(_userRepo, _unitOfWork, _passwordHasher, _jwtService);
        var regCommand = new RegisterUserCommand("login@example.com", "Password123!", "Login User", "1234567890");
        await regHandler.Handle(regCommand, CancellationToken.None);

        var loginHandler = new LoginUserCommandHandler(_userRepo, _unitOfWork, _passwordHasher, _jwtService);
        var loginCommand = new LoginUserCommand("login@example.com", "Password123!");
        var result = await loginHandler.Handle(loginCommand, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task AddItemToExistingCart_ShouldSucceed()
    {
        var cart = new Cart(Guid.NewGuid());
        _db.Carts.Add(cart);
        await _db.SaveChangesAsync();

        cart.AddItem(Guid.NewGuid(), "Test Prod", "SKU1", 100m, 1, null);
        await _db.SaveChangesAsync();

        var loadedCart = await _db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.Id == cart.Id);
        loadedCart.Should().NotBeNull();
        loadedCart!.Items.Should().HaveCount(1);
    }
}
