using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Users.Application.Abstractions;
using Users.Application.Options;
using Users.Domain.Entities;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;

namespace Users.Infrastructure.Security;

public class JwtTokenService(IOptions<JwtOptions> options) : IJwtTokenService
{
    public string GenerateToken(Guid userId, string login, UserRole role)
    {
        var claims = new[]
        {
              new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
              new Claim(ClaimTypes.Name, login),
              new Claim(ClaimTypes.Role, role.ToString())
          };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Value.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: options.Value.Issuer,
            audience: options.Value.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(options.Value.ExpiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}