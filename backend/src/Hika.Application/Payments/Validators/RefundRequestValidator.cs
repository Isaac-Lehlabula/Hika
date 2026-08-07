using FluentValidation;
using Hika.Application.Payments.Dtos;

namespace Hika.Application.Payments.Validators;

public sealed class RefundRequestValidator : AbstractValidator<RefundRequest>
{
    public RefundRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
