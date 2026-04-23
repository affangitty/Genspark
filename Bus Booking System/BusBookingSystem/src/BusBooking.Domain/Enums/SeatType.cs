namespace BusBooking.Domain.Enums;

public enum SeatType
{
    Seater = 0,
    Sleeper = 1,
    SemiSleeper = 2,
    /// <summary>Reserved for female passengers (optional product rule).</summary>
    Ladies = 3
}