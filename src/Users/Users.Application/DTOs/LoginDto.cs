using System.ComponentModel.DataAnnotations;

namespace Users.Application.DTOs;

public class LoginDto
{
    [Required] public string Login { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}