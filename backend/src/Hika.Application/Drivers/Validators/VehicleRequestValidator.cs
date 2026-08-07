using FluentValidation;
using Hika.Application.Drivers.Dtos;

namespace Hika.Application.Drivers.Validators;

public sealed class VehicleRequestValidator : AbstractValidator<VehicleRequest>
{
    public VehicleRequestValidator()
    {
        RuleFor(x => x.Make).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Model).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Year).InclusiveBetween(1980, DateTime.UtcNow.Year + 1);
        RuleFor(x => x.Color).NotEmpty().MaximumLength(30);
        RuleFor(x => x.RegistrationNumber).NotEmpty().MaximumLength(20);

        RuleFor(x => x.SeatCapacity)
            .InclusiveBetween(1, 8)
            .WithMessage("Seat capacity must be between 1 and 8.");
    }
}
