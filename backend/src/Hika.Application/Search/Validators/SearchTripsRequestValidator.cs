using FluentValidation;
using Hika.Application.Search.Dtos;

namespace Hika.Application.Search.Validators;

public sealed class SearchTripsRequestValidator : AbstractValidator<SearchTripsRequest>
{
    public SearchTripsRequestValidator()
    {
        RuleFor(x => x.From).NotEmpty().MaximumLength(100);
        RuleFor(x => x.To).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Passengers).InclusiveBetween(1, 8);
        RuleFor(x => x.MaxPrice).GreaterThan(0).When(x => x.MaxPrice.HasValue);
        RuleFor(x => x.Sort).IsInEnum();
    }
}
