using System.ComponentModel.DataAnnotations;

namespace ASP.NET_Core_Web_API.DTOs;

public class EventDTO : IValidatableObject
{
    [Required] public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    [Required] public DateTime StartDate { get; set; }
    [Required] public DateTime EndDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndDate < StartDate)
        {
            yield return new ValidationResult(
                "End date cannot be before start date",
                new[] { nameof(EndDate) });
        }
    }
}