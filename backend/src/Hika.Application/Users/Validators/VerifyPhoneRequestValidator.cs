using FluentValidation;
using Hika.Application.Users.Dtos;

namespace Hika.Application.Users.Validators;

public sealed class VerifyPhoneRequestValidator : AbstractValidator<VerifyPhoneRequest>
{
    public VerifyPhoneRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().Length(6);
    }
}
