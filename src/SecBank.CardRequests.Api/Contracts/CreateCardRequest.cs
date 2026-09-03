using System.ComponentModel.DataAnnotations;
using SecBank.CardRequests.Api.Domain;

namespace SecBank.CardRequests.Api.Contracts;

public class CreateCardRequest : IValidatableObject
{
    [Required, RegularExpression("^[0-9]{10}$", ErrorMessage = "AccountNumber must contain exactly 10 digits.")]
    public string AccountNumber { get; init; } = string.Empty;

    [Required, StringLength(120, MinimumLength = 2)]
    public string CustomerName { get; init; } = string.Empty;

    [Required, EnumDataType(typeof(CardType))]
    public CardType? CardType { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var normalizedName = (CustomerName ?? string.Empty).Trim();
        if (normalizedName.Length is < 2 or > 120)
        {
            yield return new ValidationResult(
                "CustomerName must contain between 2 and 120 non-whitespace characters.",
                [nameof(CustomerName)]);
        }
    }
}
