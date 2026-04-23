using BusBooking.Application.DTOs.Admin;
using BusBooking.Application.DTOs.Auth;
using BusBooking.Application.DTOs.Booking;
using BusBooking.Application.DTOs.Bus;
using BusBooking.Application.DTOs.Route;
using BusBooking.Application.Features.Bookings.Validators;
using FluentValidation;

namespace BusBooking.Application.Validators;

public sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequestDto>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(15);
    }
}

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequestDto>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(6).MaximumLength(100);
        RuleFor(x => x.ConfirmNewPassword).Equal(x => x.NewPassword).WithMessage("New passwords must match.");
    }
}

public sealed class UpdateOperatorProfileRequestValidator : AbstractValidator<UpdateOperatorProfileRequestDto>
{
    public UpdateOperatorProfileRequestValidator()
    {
        RuleFor(x => x.ContactPersonName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(15);
    }
}

public sealed class AddOperatorLocationRequestValidator : AbstractValidator<AddOperatorLocationRequestDto>
{
    public AddOperatorLocationRequestValidator()
    {
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AddressLine).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Landmark).MaximumLength(200);
        RuleFor(x => x.State).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PinCode).NotEmpty().MaximumLength(10);
    }
}

public sealed class BusSearchRequestValidator : AbstractValidator<BusSearchRequestDto>
{
    public BusSearchRequestValidator()
    {
        RuleFor(x => x.SourceCity).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DestinationCity).NotEmpty().MaximumLength(100);
        RuleFor(x => x.JourneyDate)
            .Must(d => d != default && DateOnly.FromDateTime(d) >= DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1))
            .WithMessage("Journey date must be today or in the future.");
        RuleFor(x => x.PassengerCount).InclusiveBetween(1, 50).When(x => x.PassengerCount.HasValue);
    }
}

public sealed class CreateBusRequestValidator : AbstractValidator<CreateBusRequestDto>
{
    public CreateBusRequestValidator()
    {
        RuleFor(x => x.BusNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.BusName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LayoutId).NotEmpty();
        RuleFor(x => x.BaseFare).GreaterThan(0).LessThanOrEqualTo(500_000);
    }
}

public sealed class UpdateFareRequestValidator : AbstractValidator<UpdateFareRequestDto>
{
    public UpdateFareRequestValidator() =>
        RuleFor(x => x.BaseFare).GreaterThan(0).LessThanOrEqualTo(500_000);
}

public sealed class CreateBusLayoutRequestValidator : AbstractValidator<CreateBusLayoutRequestDto>
{
    public CreateBusLayoutRequestValidator()
    {
        RuleFor(x => x.LayoutName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.TotalSeats).InclusiveBetween(1, 200);
        RuleFor(x => x.Rows).InclusiveBetween(1, 50);
        RuleFor(x => x.Columns).InclusiveBetween(1, 10);
        RuleFor(x => x.LayoutJson).NotEmpty();
    }
}

public sealed class AssignRouteRequestValidator : AbstractValidator<AssignRouteRequestDto>
{
    public AssignRouteRequestValidator()
    {
        RuleFor(x => x.RouteId).NotEmpty();
        RuleFor(x => x.DurationMinutes).InclusiveBetween(1, 48 * 60);
        RuleFor(x => x.BaseFare).GreaterThan(0).LessThanOrEqualTo(500_000);
    }
}

public sealed class CreateBookingRequestValidator : AbstractValidator<CreateBookingRequestDto>
{
    public CreateBookingRequestValidator()
    {
        RuleFor(x => x.BusId).NotEmpty();
        RuleFor(x => x.JourneyDate)
            .Must(d => d != default && DateOnly.FromDateTime(d) >= DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1))
            .WithMessage("Journey date must be today or in the future.");
        RuleFor(x => x.Passengers).NotEmpty();
        RuleForEach(x => x.Passengers).SetValidator(new PassengerValidator());
    }
}

public sealed class CancelBookingRequestValidator : AbstractValidator<CancelBookingRequestDto>
{
    public CancelBookingRequestValidator() =>
        RuleFor(x => x.Reason).MaximumLength(500);
}

public sealed class SeatLockRequestValidator : AbstractValidator<SeatLockRequestDto>
{
    public SeatLockRequestValidator()
    {
        RuleFor(x => x.SeatId).NotEmpty();
        RuleFor(x => x.BusId).NotEmpty();
    }
}

public sealed class CreateRouteRequestValidator : AbstractValidator<CreateRouteRequestDto>
{
    public CreateRouteRequestValidator()
    {
        RuleFor(x => x.SourceCity).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DestinationCity).NotEmpty().MaximumLength(100);
        RuleFor(x => x.SourceState).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DestinationState).NotEmpty().MaximumLength(100);
    }
}

public sealed class ApproveOperatorRequestValidator : AbstractValidator<ApproveOperatorRequestDto>
{
    public ApproveOperatorRequestValidator()
    {
        RuleFor(x => x.RejectionReason)
            .NotEmpty()
            .When(x => !x.IsApproved)
            .WithMessage("Rejection reason is required when rejecting.");
        RuleFor(x => x.RejectionReason).MaximumLength(500);
    }
}

public sealed class ApproveBusRequestValidator : AbstractValidator<ApproveBusRequestDto>
{
    public ApproveBusRequestValidator() =>
        RuleFor(x => x.AdminNotes).MaximumLength(500);
}

public sealed class PlatformConfigDtoValidator : AbstractValidator<PlatformConfigDto>
{
    public PlatformConfigDtoValidator()
    {
        RuleFor(x => x.ConvenienceFeePercentage).InclusiveBetween(0, 100);
        RuleFor(x => x.SeatLockDurationMinutes).InclusiveBetween(1, 120);
        RuleFor(x => x.FlatConvenienceFeePerPassenger).InclusiveBetween(0, 50_000);
    }
}

public sealed class DisableOperatorRequestValidator : AbstractValidator<DisableOperatorRequestDto>
{
    public DisableOperatorRequestValidator() =>
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
}

public sealed class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequestDto>
{
    public RefreshTokenRequestValidator() =>
        RuleFor(x => x.RefreshToken).NotEmpty();
}
