using AutoMapper;
using BusBooking.Domain.Entities;
using BusBooking.Application.DTOs.Auth;
using BusBooking.Application.DTOs.Bus;
using BusBooking.Application.DTOs.Booking;
using BusBooking.Application.DTOs.Route;
using BusBooking.Application.DTOs.Admin;
using Route = BusBooking.Domain.Entities.Route;

namespace BusBooking.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User
        CreateMap<User, LoginResponseDto>()
            .ForMember(d => d.FullName, o => o.MapFrom(s => s.FullName))
            .ForMember(d => d.Role, o => o.MapFrom(s => s.Role.ToString()));

        // Bus Operator
        CreateMap<BusOperator, OperatorApprovalDto>()
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()));

        // Route
        CreateMap<Route, RouteResponseDto>();
        CreateMap<CreateRouteRequestDto, Route>();

        // Bus Layout
        CreateMap<BusLayout, BusLayoutDto>();
        CreateMap<CreateBusLayoutRequestDto, BusLayout>();

        // Bus
        CreateMap<Bus, BusResponseDto>()
            .ForMember(d => d.OperatorName, o => o.MapFrom(s => s.Operator != null ? s.Operator.CompanyName : string.Empty))
            .ForMember(d => d.SourceCity, o => o.MapFrom(s => s.Route != null ? s.Route.SourceCity : string.Empty))
            .ForMember(d => d.DestinationCity, o => o.MapFrom(s => s.Route != null ? s.Route.DestinationCity : string.Empty))
            .ForMember(d => d.TotalSeats, o => o.MapFrom(s => s.Layout != null ? s.Layout.TotalSeats : 0))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.AvailableSeats, o => o.Ignore())
            .ForMember(d => d.ConvenienceFee, o => o.Ignore())
            .ForMember(d => d.TotalFare, o => o.Ignore());

        CreateMap<CreateBusRequestDto, Bus>();

        // Seat
        CreateMap<Seat, SeatDto>()
            .ForMember(d => d.SeatType, o => o.MapFrom(s => s.SeatType.ToString()))
            .ForMember(d => d.IsAvailable, o => o.Ignore())
            .ForMember(d => d.IsLocked, o => o.Ignore());

        // Booking
        CreateMap<Booking, BookingResponseDto>()
            .ForMember(d => d.BusNumber, o => o.MapFrom(s => s.Bus != null ? s.Bus.BusNumber : string.Empty))
            .ForMember(d => d.BusName, o => o.MapFrom(s => s.Bus != null ? s.Bus.BusName : string.Empty))
            .ForMember(d => d.SourceCity, o => o.MapFrom(s => s.Bus != null && s.Bus.Route != null ? s.Bus.Route.SourceCity : string.Empty))
            .ForMember(d => d.DestinationCity, o => o.MapFrom(s => s.Bus != null && s.Bus.Route != null ? s.Bus.Route.DestinationCity : string.Empty))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()));

        CreateMap<BookingPassenger, PassengerResponseDto>();

        // Cancellation
        CreateMap<Cancellation, CancellationResponseDto>()
            .ForMember(d => d.BookingReference, o => o.MapFrom(s => s.Booking != null ? s.Booking.BookingReference : string.Empty));

        // Platform Config
        CreateMap<PlatformConfig, PlatformConfigDto>();
        CreateMap<PlatformConfigDto, PlatformConfig>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.UpdatedAt, o => o.Ignore())
            .ForMember(d => d.UpdatedByAdminId, o => o.Ignore());
    }
}