using FluentValidation;
using Hika.Application.TrustSafety.Dtos;
using Hika.Application.Users.Validators;

namespace Hika.Application.TrustSafety.Validators;

public sealed class EmergencyContactRequestValidator : AbstractValidator<EmergencyContactRequest>
{
    public EmergencyContactRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PhoneNumber).Must(phone => SouthAfricanPhoneNumber.Pattern().IsMatch(phone))
            .WithMessage("Enter a valid South African phone number, e.g. +27821234567.");
        RuleFor(x => x.Relationship).MaximumLength(50);
    }
}
