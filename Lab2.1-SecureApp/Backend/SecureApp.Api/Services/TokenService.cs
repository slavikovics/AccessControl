using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SecureApp.Api.Models;

namespace SecureApp.Api.Services;

public class TokenService(IConfiguration configuration)
{
    private readonly string _key = configuration["Jwt:Key"]
        ?? throw new InvalidOperationException("Jwt:Key is not configured.");
    private readonly string _issuer = configuration["Jwt:Issuer"] ?? "SecureApp";

    public string CreateToken(User user, out DateTime expiresUtc)
    {
        expiresUtc = DateTime.UtcNow.AddMinutes(30);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _issuer,
            claims: claims,
            expires: expiresUtc,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
