using MediatR;
using Shop.Application.Common.Interfaces;
using Shop.Application.DTOs;
using Shop.Domain.Common;
using Shop.Domain.Entities;
using Shop.Domain.Repositories;

namespace Shop.Application.Features.Auth.Commands;

public record RegisterUserCommand(string Email, string Password, string FullName, string? PhoneNumber) : IRequest<Result<AuthResponseDto>>;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<AuthResponseDto>>
{
    private readonly IUserRepository _userRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtService;

    public RegisterUserCommandHandler(
        IUserRepository userRepo,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtService)
    {
        _userRepo = userRepo;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
    }

    public async Task<Result<AuthResponseDto>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var existing = await _userRepo.GetByEmailAsync(request.Email, cancellationToken);
        if (existing != null)
        {
            return Result<AuthResponseDto>.Failure(new Error("Auth.EmailExists", "A user with this email already exists."));
        }

        var passwordHash = _passwordHasher.HashPassword(request.Password);
        var user = new User(request.Email, request.FullName, passwordHash, "Customer", request.PhoneNumber);

        await _userRepo.AddAsync(user, cancellationToken);
        await _unitOfWork.CommitChangesAsync(cancellationToken);

        var tokens = _jwtService.GenerateTokens(user.Id, user.Email, user.FullName, user.Role);
        user.AddRefreshToken(tokens.RefreshToken, tokens.ExpiresAt.AddDays(7));
        await _unitOfWork.CommitChangesAsync(cancellationToken);

        return Result<AuthResponseDto>.Success(tokens);
    }
}

public record LoginUserCommand(string Email, string Password) : IRequest<Result<AuthResponseDto>>;

public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, Result<AuthResponseDto>>
{
    private readonly IUserRepository _userRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtService;

    public LoginUserCommandHandler(
        IUserRepository userRepo,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtService)
    {
        _userRepo = userRepo;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
    }

    public async Task<Result<AuthResponseDto>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepo.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return Result<AuthResponseDto>.Failure(new Error("Auth.InvalidCredentials", "Invalid email or password."));
        }

        if (!user.IsActive)
        {
            return Result<AuthResponseDto>.Failure(new Error("Auth.UserDeactivated", "This account is deactivated."));
        }

        user.RecordLogin();
        var tokens = _jwtService.GenerateTokens(user.Id, user.Email, user.FullName, user.Role);
        user.AddRefreshToken(tokens.RefreshToken, tokens.ExpiresAt.AddDays(7));

        await _unitOfWork.CommitChangesAsync(cancellationToken);

        return Result<AuthResponseDto>.Success(tokens);
    }
}

public record RefreshTokenCommand(string RefreshToken) : IRequest<Result<AuthResponseDto>>;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponseDto>>
{
    private readonly IUserRepository _userRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenService _jwtService;

    public RefreshTokenCommandHandler(IUserRepository userRepo, IUnitOfWork unitOfWork, IJwtTokenService jwtService)
    {
        _userRepo = userRepo;
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
    }

    public async Task<Result<AuthResponseDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepo.GetByRefreshTokenAsync(request.RefreshToken, cancellationToken);
        if (user == null)
        {
            return Result<AuthResponseDto>.Failure(new Error("Auth.InvalidToken", "Invalid refresh token."));
        }

        var rt = user.RefreshTokens.FirstOrDefault(t => t.Token == request.RefreshToken && t.IsActive);
        if (rt == null)
        {
            return Result<AuthResponseDto>.Failure(new Error("Auth.ExpiredToken", "Refresh token is expired or revoked."));
        }

        rt.Revoke();
        var tokens = _jwtService.GenerateTokens(user.Id, user.Email, user.FullName, user.Role);
        user.AddRefreshToken(tokens.RefreshToken, tokens.ExpiresAt.AddDays(7));

        await _unitOfWork.CommitChangesAsync(cancellationToken);
        return Result<AuthResponseDto>.Success(tokens);
    }
}
