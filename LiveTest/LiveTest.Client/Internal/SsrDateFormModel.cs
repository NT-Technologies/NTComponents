using System.ComponentModel.DataAnnotations;

namespace LiveTest.Client.Internal;

/// <summary>
///     Form model for the static SSR date picker form demo.
/// </summary>
public class SsrDateFormModel : IValidatableObject {

    [Required(ErrorMessage = "Pick a start date.")]
    public DateOnly? Start { get; set; }

    [Required(ErrorMessage = "Pick an end date.")]
    public DateOnly? End { get; set; }

    [Required(ErrorMessage = "Pick a departure date.")]
    public DateOnly? Departure { get; set; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
        if (Start is { } start && End is { } end && end.DayNumber - start.DayNumber > 14) {
            yield return new ValidationResult("Trips can be at most 14 days.", [nameof(End)]);
        }
    }
}
