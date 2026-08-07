using FluentValidation;
using Hika.Application.Admin.Dtos;

namespace Hika.Application.Admin.Validators;

public sealed class RemoveTripRequestValidator : AbstractValidator<RemoveTripRequest>
{
    public RemoveTripRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
