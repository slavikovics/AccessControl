using System.ComponentModel.DataAnnotations;

namespace SecureApp.Api.Dtos;

public record RegisterRequest(
    [Required, MinLength(3), MaxLength(32)] string Username,
    [Required, MinLength(8), MaxLength(128)] string Password);

public record LoginRequest(
    [Required] string Username,
    [Required] string Password);

public record MeResponse(int Id, string Username);
