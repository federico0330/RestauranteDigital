using System.ComponentModel.DataAnnotations;

namespace Restaurante.Application.DTOs;

public record LoginRequest([Required] string Email, [Required] string Password);

public record RegisterRequest(
    [Required] string Name,
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password);

public record AuthResponse(int UserId, string Name, string Email, string Role, string Token);
