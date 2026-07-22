using System.ComponentModel.DataAnnotations;
using ASP.NET_Core_Web_API.Models;

namespace ASP.NET_Core_Web_API.DTOs;

public class EventDTO : IValidatableObject
{
    [Required][MinLength(1)] public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    [Required] public DateTime StartAt { get; set; }
    [Required] public DateTime EndAt { get; set; }


    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndAt <= StartAt)
        {
            yield return new ValidationResult(
                "End date cannot be before start date",
                new[] { nameof(EndAt) });
        }
    }
}