using FluentValidation;
using Hika.Application.RideRequests.Dtos;

namespace Hika.Application.RideRequests.Validators;

public sealed class CreateRideRequestRequestValidator : AbstractValidator<CreateRideRequestRequest>
{
    public CreateRideRequestRequestValidator()
    {
        RuleFor(x => x.Origin).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Destination).NotEmpty().MaximumLength(100);
        RuleFor(x => x.TravelDate).GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow.Date)).WithMessage("Travel date can't be in the past.");
        RuleFor(x => x.SeatsNeeded).GreaterThanOrEqualTo(1);
        RuleFor(x => x.ProposedPricePerSeat).GreaterThanOrEqualTo(0).When(x => x.ProposedPricePerSeat.HasValue);
    }
}
