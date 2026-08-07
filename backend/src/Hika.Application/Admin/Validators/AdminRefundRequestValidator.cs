using FluentValidation;
using Hika.Application.Admin.Dtos;

namespace Hika.Application.Admin.Validators;

public sealed class AdminRefundRequestValidator : AbstractValidator<AdminRefundRequest>
{
    public AdminRefundRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
