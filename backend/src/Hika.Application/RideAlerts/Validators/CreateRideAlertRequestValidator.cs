using FluentValidation;
using Hika.Application.RideAlerts.Dtos;

namespace Hika.Application.RideAlerts.Validators;

public sealed class CreateRideAlertRequestValidator : AbstractValidator<CreateRideAlertRequest>
{
    public CreateRideAlertRequestValidator()
    {
        RuleFor(x => x.Origin).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Destination).NotEmpty().MaximumLength(100);
    }
}
