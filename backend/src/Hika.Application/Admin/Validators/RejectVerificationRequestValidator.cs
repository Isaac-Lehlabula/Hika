using FluentValidation;
using Hika.Application.Admin.Dtos;

namespace Hika.Application.Admin.Validators;

public sealed class RejectVerificationRequestValidator : AbstractValidator<RejectVerificationRequest>
{
    public RejectVerificationRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
