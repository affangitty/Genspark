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
        // Date-only from the client can disagree with "UTC today" near the dateline; allow one UTC-day slack.
        RuleFor(x => x.JourneyDate)
            .Must(d => d != default && DateOnly.FromDateTime(d) >= DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1))
            .WithMessage("Journey date must be today or in the future.");
        RuleFor(x => x.PassengerCount)
            .InclusiveBetween(1, 50)
            .When(x => x.PassengerCount.HasValue);
    }
}
