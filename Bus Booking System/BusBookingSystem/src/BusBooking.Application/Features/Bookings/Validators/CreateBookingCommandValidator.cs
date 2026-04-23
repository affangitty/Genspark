using BusBooking.Application.Features.Bookings.Commands;
using FluentValidation;

namespace BusBooking.Application.Features.Bookings.Validators;

public class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingCommandValidator()
    {
        RuleFor(x => x.BusId)
            .NotEmpty().WithMessage("Bus ID is required");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.JourneyDate)
            .NotEmpty().WithMessage("Journey date is required")
            .GreaterThanOrEqualTo(DateTime.UtcNow.Date).WithMessage("Journey date must be in the future");

        RuleFor(x => x.Passengers)
            .NotEmpty().WithMessage("At least one passenger is required")
            .Must(p => p.Count > 0).WithMessage("At least one passenger is required");

        RuleForEach(x => x.Passengers)
            .SetValidator(new PassengerValidator());
    }
}

public class PassengerValidator : AbstractValidator<BusBooking.Application.DTOs.Booking.PassengerDto>
{
    public PassengerValidator()
    {
        RuleFor(x => x.PassengerName)
            .NotEmpty().WithMessage("Passenger name is required")
            .MinimumLength(2).WithMessage("Passenger name must be at least 2 characters");

        RuleFor(x => x.Age)
            .GreaterThan(0).WithMessage("Age must be greater than 0")
            .LessThan(150).WithMessage("Age must be less than 150");

        RuleFor(x => x.Gender)
            .NotEmpty().WithMessage("Gender is required")
            .Must(g => g == "Male" || g == "Female" || g == "Other")
            .WithMessage("Gender must be Male, Female, or Other");

        RuleFor(x => x.SeatId)
            .NotEmpty().WithMessage("Seat selection is required");
    }
}
