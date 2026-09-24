using System.ComponentModel.DataAnnotations;

namespace EventApi.Application.DTOs;

public class LoginDto
{
    [Required] public string Login { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}