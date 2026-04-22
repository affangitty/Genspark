using BusBooking.Domain.Common;
using BusBooking.Domain.Enums;

namespace BusBooking.Domain.Entities;

public class Seat : BaseEntity
{
    public Guid BusId { get; set; }
    public string SeatNumber { get; set; } = string.Empty;  // e.g. "A1", "B2"
    public int Row { get; set; }
    public int Column { get; set; }
    public string Deck { get; set; } = "lower";             // "lower" | "upper"
    public SeatType SeatType { get; set; } = SeatType.Seater;
    public bool IsActive { get; set; } = true;

    // Navigation
    public Bus Bus { get; set; } = null!;
    public ICollection<BookingPassenger> BookingPassengers { get; set; } = new List<BookingPassenger>();
    public ICollection<SeatLock> SeatLocks { get; set; } = new List<SeatLock>();
}