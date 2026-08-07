using FluentValidation;
using Hika.Application.Trips.Dtos;

namespace Hika.Application.Trips.Validators;

public sealed class CreateTripRequestValidator : AbstractValidator<CreateTripRequest>
{
    public CreateTripRequestValidator()
    {
        RuleFor(x => x.VehicleId).NotEmpty();

        RuleFor(x => x.DepartureAtUtc)
            .GreaterThan(DateTimeOffset.UtcNow)
            .WithMessage("Departure must be in the future.");

        RuleFor(x => x.TotalSeatsOffered).InclusiveBetween(1, 8);

        RuleFor(x => x.PricePerSeat).GreaterThan(0);

        RuleFor(x => x.LuggageAllowance).MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(1000);

        RuleFor(x => x.Stops)
            .Must(stops => stops.Count >= 2)
            .WithMessage("A trip needs at least an origin and a destination stop.");

        RuleForEach(x => x.Stops).ChildRules(stop =>
        {
            stop.RuleFor(s => s.RawName).NotEmpty().MaximumLength(100);
        });
    }
}
