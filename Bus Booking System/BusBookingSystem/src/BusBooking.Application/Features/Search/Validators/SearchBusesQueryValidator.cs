using BusBooking.Application.Features.Search.Queries;
using FluentValidation;

namespace BusBooking.Application.Features.Search.Validators;

public sealed class SearchBusesQueryValidator : AbstractValidator<SearchBusesQuery>
{
    public SearchBusesQueryValidator()
    {
        RuleFor(x => x.SourceCity)
            .NotEmpty().MaximumLength(100);
        RuleFor(x => x.DestinationCity)
            .NotEmpty().MaximumLength(100);
        RuleFor(x => x.JourneyDate)
            .Must(d => d.Date >= DateTime.UtcNow.Date)
            .WithMessage("Journey date must be today or in the future.");
        RuleFor(x => x.PassengerCount)
            .InclusiveBetween(1, 50)
            .When(x => x.PassengerCount.HasValue);
    }
}
