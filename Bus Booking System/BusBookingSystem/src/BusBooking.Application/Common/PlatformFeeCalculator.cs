using BusBooking.Domain.Entities;

namespace BusBooking.Application.Common;

public static class PlatformFeeCalculator
{
    public static decimal ComputeConvenienceFee(PlatformConfig? cfg, decimal baseFareTotal, int passengerCount)
    {
        passengerCount = Math.Max(1, passengerCount);
        if (cfg == null)
            return Math.Round(baseFareTotal * 5m / 100m, 2);

        if (cfg.UseFlatConvenienceFee)
            return Math.Round(Math.Max(0, cfg.FlatConvenienceFeePerPassenger) * passengerCount, 2);

        return Math.Round(baseFareTotal * cfg.ConvenienceFeePercentage / 100m, 2);
    }

    /// <summary>Convenience for one seat at listed base fare (search cards).</summary>
    public static decimal ComputeConvenienceForSingleSeat(PlatformConfig? cfg, decimal baseFarePerSeat)
    {
        if (cfg == null)
            return Math.Round(baseFarePerSeat * 5m / 100m, 2);

        if (cfg.UseFlatConvenienceFee)
            return Math.Round(Math.Max(0, cfg.FlatConvenienceFeePerPassenger), 2);

        return Math.Round(baseFarePerSeat * cfg.ConvenienceFeePercentage / 100m, 2);
    }
}
