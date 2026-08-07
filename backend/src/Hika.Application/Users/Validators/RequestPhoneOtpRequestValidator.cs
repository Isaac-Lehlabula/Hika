using FluentValidation;
using Hika.Application.Users.Dtos;

namespace Hika.Application.Users.Validators;

public sealed class RequestPhoneOtpRequestValidator : AbstractValidator<RequestPhoneOtpRequest>
{
    public RequestPhoneOtpRequestValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .Must(phone => SouthAfricanPhoneNumber.Pattern().IsMatch(phone))
            .WithMessage("Phone number must be a valid South African number in +27 format.");
    }
}
