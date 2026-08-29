using FluentValidation;
using Hika.Application.Notifications.Dtos;

namespace Hika.Application.Notifications.Validators;

public sealed class RegisterDeviceTokenRequestValidator : AbstractValidator<RegisterDeviceTokenRequest>
{
    public RegisterDeviceTokenRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(4096);
        RuleFor(x => x.Platform).IsInEnum();
    }
}
