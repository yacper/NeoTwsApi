// created: 2022/06/22 18:17
// author:  rush
// email:   yacper@gmail.com
// 
// purpose:
// modifiers:

using NeoTwsApi.Enums;

namespace NeoTwsApi.Helpers;

public class DurationTws
{
    public EDurationStep Step { get; protected set; }
    public int Quantity { get; protected set; }

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