using FluentValidation;
using Hika.Application.TrustSafety.Dtos;

namespace Hika.Application.TrustSafety.Validators;

public sealed class CreateReportRequestValidator : AbstractValidator<CreateReportRequest>
{
    public CreateReportRequestValidator()
    {
        RuleFor(x => x.Reason).IsInEnum();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
        RuleFor(x => x)
            .Must(x => x.ReportedUserId.HasValue || x.ReportedTripId.HasValue)
            .WithMessage("Specify who or what you're reporting.")
            .WithName("reportedUserId");
    }
}
