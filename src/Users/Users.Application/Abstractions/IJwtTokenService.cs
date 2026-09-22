using Users.Domain.Entities;

namespace Users.Application.Abstractions;

public interface IJwtTokenService
{
    string GenerateToken(Guid userId, string login, UserRole role);
}