using FluentValidation;
using Hika.Application.Drivers.Dtos;

namespace Hika.Application.Drivers.Validators;

public sealed class CreateOrUpdateDriverProfileRequestValidator : AbstractValidator<CreateOrUpdateDriverProfileRequest>
{
    public CreateOrUpdateDriverProfileRequestValidator()
    {
        RuleFor(x => x.LicenseNumber).NotEmpty().MaximumLength(30);

        RuleFor(x => x.LicenseExpiryDate)
            .GreaterThan(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("License expiry date must be in the future.");
    }
}
