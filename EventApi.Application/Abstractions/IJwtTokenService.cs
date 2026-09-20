using EventApi.Domain.Entities;

namespace EventApi.Application.Abstractions;

public interface IJwtTokenService
{
    string GenerateToken(Guid userId, string login, UserRole role);   
}