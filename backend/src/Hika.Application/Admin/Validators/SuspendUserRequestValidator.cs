using FluentValidation;
using Hika.Application.Admin.Dtos;

namespace Hika.Application.Admin.Validators;

public sealed class SuspendUserRequestValidator : AbstractValidator<SuspendUserRequest>
{
    public SuspendUserRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
