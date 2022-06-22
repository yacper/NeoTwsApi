// created: 2022/06/22 18:17
// author:  rush
// email:   yacper@gmail.com
// 
// purpose:
// modifiers:

using NeoTwsApi.Enums;

namespace NeoTwsApi.Helpers;

public static class TwsDuration
{
    public static string ToTwsDuration(DateTime start, DateTime end, ETimeFrameTws tf)
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