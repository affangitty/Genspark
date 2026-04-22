using BusBooking.Domain.Common;

namespace BusBooking.Domain.Entities;

/// <summary>
/// Defines the physical layout of a bus (rows, columns, deck config).
/// Stored as JSON so it can flex per operator.
/// </summary>
public class BusLayout : BaseEntity
{
    public string LayoutName { get; set; } = string.Empty;   // e.g. "2+2 Seater", "1+2 Sleeper"
    public int TotalSeats { get; set; }
    public int Rows { get; set; }
    public int Columns { get; set; }
    public bool HasUpperDeck { get; set; } = false;

    /// <summary>
    /// JSON array describing each seat: 
    /// [{ "seatNumber": "A1", "row": 1, "column": 1, "deck": "lower", "type": "Seater" }, ...]
    /// </summary>
    public string LayoutJson { get; set; } = "[]";

    public Guid OperatorId { get; set; }

    // Navigation
    public BusOperator Operator { get; set; } = null!;
    public ICollection<Bus> Buses { get; set; } = new List<Bus>();
}