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
    public static DurationTws ToDurationTws(DateTime start, DateTime end, ETimeFrameTws tf)
    {// 大部分情况下，只能转换成以D为表示的区间，s不能多于86400，也就是不能多于1天
        // todo:
        // tws在分钟级别的时候，如果设置duration 1天，获得的数据很可能是少于一天的，
        // 这个跟时区有关,虽然设置了duration为1天，但是时区不同，造成获取的数据可能会少，这里简单处理下+1天
        TimeSpan ts = end - start;
        switch (tf)
        {
            case < ETimeFrameTws.M1 and >=ETimeFrameTws.S1:
                return new DurationTws((int)Math.Ceiling(ts.TotalSeconds), EDurationStep.S);
            case < ETimeFrameTws.H1 and >=ETimeFrameTws.M1:
                return new DurationTws((int)Math.Ceiling(ts.TotalDays+1), EDurationStep.D);
            case <= ETimeFrameTws.D1 and >=ETimeFrameTws.H1:
                return new DurationTws((int)Math.Ceiling(ts.TotalDays), EDurationStep.D);
             case <= ETimeFrameTws.MN1 and >=ETimeFrameTws.W1:
                return new DurationTws((int)Math.Ceiling(ts.TotalDays), EDurationStep.D);
        }

        throw new NotImplementedException();
    }

    
}