using FluentValidation;
using Hika.Application.Admin.Dtos;

namespace Hika.Application.Admin.Validators;

public sealed class UpdatePlatformFeeRequestValidator : AbstractValidator<UpdatePlatformFeeRequest>
{
    public UpdatePlatformFeeRequestValidator()
    {
        RuleFor(x => x.Rate).InclusiveBetween(0m, 1m);
    }
}
