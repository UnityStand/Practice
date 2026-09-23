using System.ComponentModel.DataAnnotations;
using Users.Domain.Entities;

namespace Users.Application.DTOs;

public class RegisterDto
{
    [Required] public string Login { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Customer;
}
