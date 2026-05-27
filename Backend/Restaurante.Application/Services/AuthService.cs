using Restaurante.Application.DTOs;
using Restaurante.Application.Interfaces;
using Restaurante.Domain.Entities;
using Restaurante.Domain.Interfaces;

namespace Restaurante.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;

    public AuthService(IUnitOfWork uow, IPasswordHasher hasher, IJwtTokenService jwt)
    {
        _uow = uow;
        _hasher = hasher;
        _jwt = jwt;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _uow.Users.GetByEmailAsync(request.Email);
        if (user is null) return null;
        if (!_hasher.Verify(request.Password, user.PasswordHash)) return null;

        var token = _jwt.GenerateToken(user.Id, user.Email, user.Role);
        return new AuthResponse(user.Id, user.Name, user.Email, user.Role, token);
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        if (await _uow.Users.EmailExistsAsync(request.Email)) return null;

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            PasswordHash = _hasher.Hash(request.Password),
            Role = UserRoles.User,
            CreateDate = DateTime.UtcNow
        };

        await _uow.Users.AddAsync(user);
        await _uow.SaveChangesAsync();

        var token = _jwt.GenerateToken(user.Id, user.Email, user.Role);
        return new AuthResponse(user.Id, user.Name, user.Email, user.Role, token);
    }
}
