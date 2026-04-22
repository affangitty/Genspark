namespace BusBooking.Application.DTOs.Admin;

public class ApproveOperatorRequestDto
{
    public bool IsApproved { get; set; }
    public string? RejectionReason { get; set; }
}

public class ApproveBusRequestDto
{
    public bool IsApproved { get; set; }
    public string? AdminNotes { get; set; }
}

public class PlatformConfigDto
{
    public decimal ConvenienceFeePercentage { get; set; }
    public int SeatLockDurationMinutes { get; set; }
}

public class RevenueReportDto
{
    public Guid OperatorId { get; set; }
    public string OperatorName { get; set; } = string.Empty;
    public decimal TotalRevenue { get; set; }
    public decimal PlatformRevenue { get; set; }
    public int TotalBookings { get; set; }
    public int CancelledBookings { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class OperatorApprovalDto
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string ContactPersonName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class DisableOperatorRequestDto
{
    public string Reason { get; set; } = string.Empty;
}