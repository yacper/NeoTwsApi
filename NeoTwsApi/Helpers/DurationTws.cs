// created: 2022/06/22 18:17
// author:  rush
// email:   yacper@gmail.com
// 
// purpose:
// modifiers:

using NeoTwsApi.Enums;
using NLog.Time;

namespace NeoTwsApi.Helpers;

public class DurationTws
{
    public EDurationStep Step { get; set; }
    public int Quantity { get; set; }

    // https://interactivebrokers.github.io/tws-api/historical_limitations.html
    // the doc is probably outdated, 86400s(24hr) can have S5 time frame data, which returns 17280 candles
    public bool CheckValid(ETimeFrameTws tf)            // check if the duration is valid for particular time frame
    {
        if (Quantity <= 0)
            return false;

        switch (Step)
        {
            case EDurationStep.S:
                switch (Quantity)
                {
                    case <= 60:
                        return tf >= ETimeFrameTws.S1 && tf <= ETimeFrameTws.M1;
                    case > 60 and <= 120:
                        return tf >= ETimeFrameTws.S1 && tf <= ETimeFrameTws.M2;
                    case > 120 and <= 1800:
                        return tf >= ETimeFrameTws.S1 && tf <= ETimeFrameTws.M30;
                    case > 1800 and <= 3600: // 1hr
                        return tf >= ETimeFrameTws.S5 && tf <= ETimeFrameTws.H1;
                    case > 3600 and <= 14400: // 4hr
                        return tf >= ETimeFrameTws.S5 && tf <= ETimeFrameTws.H3;
                    case > 14400 and <= 28800: // 8hr
                        return tf >= ETimeFrameTws.S5 && tf <= ETimeFrameTws.H8;
                    case > 28800 and <= 86400: // >8hr
                        return tf >= ETimeFrameTws.S5 && tf <= ETimeFrameTws.H8;
                    default:
                        return false; // Step S can't have more than 86400
                }
            case >= EDurationStep.D and <= EDurationStep.Y:
                return tf >= ETimeFrameTws.S5;
        }

        return true;
    }

    public override string ToString()  => $"{Quantity} {Step}"; 

    public DurationTws(int quantity, EDurationStep step)
    {
        Quantity = quantity;
        Step = step;
    }
}

public static class DurationTwsEx
{
    /// <summary>
    ///https://interactivebrokers.github.io/tws-api/historical_limitations.html#hd_step_sizes 
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="tf"></param>
    /// <returns></returns>
    public static string ToDurationTws(DateTime start, DateTime end, ETimeFrameTws tf)
    {
        TimeSpan ts = end - start;

        return $"{Math.Ceiling(ts.TotalSeconds)} S";

        //return tf switch
        //       {
        //           >=ETimeFrameTws.S1 and <= ETimeFrameTws.H8 => $"{Math.Ceiling(ts.TotalSeconds)} S",
        //           >=ETimeFrameTws.M1 and <= ETimeFrameTws.M30 => $"{ts.TotalMinutes} S",

        //       }

    }

    
}