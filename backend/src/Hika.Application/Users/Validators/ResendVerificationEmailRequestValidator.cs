using FluentValidation;
using Hika.Application.Users.Dtos;

namespace Hika.Application.Users.Validators;

public sealed class ResendVerificationEmailRequestValidator : AbstractValidator<ResendVerificationEmailRequest>
{
    public ResendVerificationEmailRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
