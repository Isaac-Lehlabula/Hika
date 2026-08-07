using FluentValidation;
using Hika.Application.Bookings.Dtos;

namespace Hika.Application.Bookings.Validators;

public sealed class CreateBookingRequestValidator : AbstractValidator<CreateBookingRequest>
{
    public CreateBookingRequestValidator()
    {
        RuleFor(x => x.TripId).NotEmpty();
        RuleFor(x => x.BoardingStopSequence).GreaterThanOrEqualTo(0);
        RuleFor(x => x.AlightingStopSequence).GreaterThan(x => x.BoardingStopSequence)
            .WithMessage("The alighting stop must come after the boarding stop.");
        RuleFor(x => x.SeatsRequested).InclusiveBetween(1, 8);
    }
}
