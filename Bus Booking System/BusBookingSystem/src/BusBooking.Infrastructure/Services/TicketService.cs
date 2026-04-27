using BusBooking.Application.Interfaces;
using BusBooking.Domain.Entities;
using BusBooking.Domain.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BusBooking.Infrastructure.Services;

public class TicketService : ITicketService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly string _ticketStoragePath;

    static TicketService() =>
        QuestPDF.Settings.License = LicenseType.Community;

    public TicketService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _ticketStoragePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tickets");

        if (!Directory.Exists(_ticketStoragePath))
            Directory.CreateDirectory(_ticketStoragePath);
    }

    public async Task<byte[]> GenerateTicketAsync(Guid bookingId)
    {
        var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
        if (booking == null)
            return Array.Empty<byte>();

        return BuildPdf(booking);
    }

    public async Task<string> GenerateTicketFilePathAsync(Guid bookingId)
    {
        var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
        if (booking == null)
            return string.Empty;

        var bytes = BuildPdf(booking);
        var fileName = $"Ticket_{booking.BookingReference}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
        var filePath = Path.Combine(_ticketStoragePath, fileName);
        await File.WriteAllBytesAsync(filePath, bytes);
        return filePath;
    }

    private static byte[] BuildPdf(Booking booking)
    {
        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Margin(36);
                page.Size(PageSizes.A4);
                page.Header().Text("Bus Booking Ticket").SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);
                page.Content().Column(column =>
                {
                    column.Spacing(10);
                    column.Item().Text($"Booking reference: {booking.BookingReference}").SemiBold();
                    column.Item().Text($"Issued (UTC): {DateTime.UtcNow:dd MMM yyyy HH:mm}");
                    column.Item().LineHorizontal(1).LineColor(Colors.Grey.Medium);

                    column.Item().Text("Bus details").SemiBold().FontSize(14);
                    column.Item().Text($"Bus number: {booking.Bus.BusNumber}");
                    column.Item().Text($"Bus name: {booking.Bus.BusName}");
                    column.Item().Text($"Operator: {booking.Bus.Operator.CompanyName}");

                    column.Item().Text("Journey").SemiBold().FontSize(14);
                    column.Item().Text(
                        $"From: {booking.Bus.Route?.SourceCity ?? "—"}  To: {booking.Bus.Route?.DestinationCity ?? "—"}");
                    column.Item().Text($"Journey date: {booking.JourneyDate:dd MMM yyyy}");
                    column.Item().Text($"Departure: {FormatTime(booking.Bus.DepartureTime)}  Arrival: {FormatTime(booking.Bus.ArrivalTime)}");

                    column.Item().Text("Boarding & drop-off").SemiBold().FontSize(14);
                    column.Item().Text($"Boarding: {booking.BoardingAddress}");
                    column.Item().Text($"Drop-off: {booking.DropOffAddress}");

                    column.Item().Text("Passengers").SemiBold().FontSize(14);
                    foreach (var p in booking.Passengers)
                    {
                        column.Item().Background(Colors.Grey.Lighten3).Padding(8).Column(pc =>
                        {
                            pc.Item().Text($"{p.PassengerName} — Seat {p.Seat.SeatNumber} ({p.Gender}, age {p.Age})");
                        });
                    }

                    column.Item().Text("Pricing").SemiBold().FontSize(14);
                    column.Item().Text($"Base fare (×{booking.Passengers.Count}): ₹{booking.BaseFareTotal:F2}");
                    column.Item().Text($"Convenience fee: ₹{booking.ConvenienceFee:F2}");
                    column.Item().Text($"Total: ₹{booking.TotalAmount:F2}").SemiBold().FontSize(16).FontColor(Colors.Blue.Darken2);

                    column.Item().Text($"Status: {booking.Status}");
                    column.Item().Text($"Booked on (UTC): {booking.CreatedAt:dd MMM yyyy HH:mm}");
                });
            });
        }).GeneratePdf();
    }

    private static string FormatTime(TimeSpan? t) =>
        t == null ? "—" : DateTime.Today.Add(t.Value).ToString("hh:mm tt");
}
