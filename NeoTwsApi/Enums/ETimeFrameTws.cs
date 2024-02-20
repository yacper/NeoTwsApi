/********************************************************************
    created: 2022/06/22 16:09
    author:  rush
    email:  yacper@gmail.com

    purpose:
    modifiers:
    ref:https://interactivebrokers.github.io/tws-api/historical_bars.html#hd_barsize  
*********************************************************************/

namespace NeoTwsApi.Enums;

//Valid Bar Sizes
//Size
//1 secs	5 secs	10 secs	15 secs	30 secs
//1 min	2 mins	3 mins	5 mins	10 mins	15 mins	20 mins	30 mins
//1 hour	2 hours	3 hours	4 hours	8 hours
//1 day
//1 week
//1 month
// https://interactivebrokers.github.io/tws-api/historical_bars.html
public enum ETimeFrameTws
{
    Unknown = 0,
    S1,
    S5,
    S10,
    S15,
    S30,

    M1,
    M2,
    M3,
    M5,
    M10,
    M15,
    M20,
    M30,

    H1, // 1小时
    H2,
    H3,
    H4,
    H8,

    D1,
    W1,
    MN1,
    //Y1,   //不支持年线
}

public static class ETimeFrameTwsEx
{
    public static string ToTwsString(this ETimeFrameTws tf)
    {
        switch (tf)
        {
            case ETimeFrameTws.Unknown:
                throw new ArgumentException();
            case >= ETimeFrameTws.S1 and <= ETimeFrameTws.S30:
                return $"{tf.ToString().TrimStart('S')} secs";

            case ETimeFrameTws.M1:
                return $"1 min";
            case >= ETimeFrameTws.M2 and <= ETimeFrameTws.M30:
                return $"{tf.ToString().TrimStart('M')} mins";

            case ETimeFrameTws.H1:
                return $"1 hour";
            case >= ETimeFrameTws.H2 and <= ETimeFrameTws.H8:
                return $"{tf.ToString().TrimStart('H')} hours";

            case ETimeFrameTws.D1:
                return $"1 day";

            case ETimeFrameTws.W1:
                return $"1 week";

            case ETimeFrameTws.MN1:
                return $"1 month";

                // 不支持年线
            //case ETimeFrameTws.Y1:
            //    return $"1 year";

        }

        throw new ArgumentException();
    }

    public static ETimeFrameTws ToETimeFrameTws(this string tf)
    {
        switch (tf)
        {
            case string s when s.Contains("sec"):
                return s.Split(' ')[0] switch
                {
                    "1"  => ETimeFrameTws.S1,
                    "5"  => ETimeFrameTws.S5,
                    "10" => ETimeFrameTws.S10,
                    "15" => ETimeFrameTws.S15,
                    "30" => ETimeFrameTws.S30,
                };
                break;

            case string s when s.Contains("min"):
                return s.Split(' ')[0] switch
                {
                    "1"  => ETimeFrameTws.M1,
                    "2"  => ETimeFrameTws.M2,
                    "3"  => ETimeFrameTws.M3,
                    "5"  => ETimeFrameTws.M5,
                    "10" => ETimeFrameTws.M10,
                    "15" => ETimeFrameTws.M15,
                    "20" => ETimeFrameTws.M20,
                    "30" => ETimeFrameTws.M30,
                };
                break;

            case string s when s.Contains("hour"):
                return s.Split(' ')[0] switch
                {
                    "1" => ETimeFrameTws.H1,
                    "2" => ETimeFrameTws.H2,
                    "3" => ETimeFrameTws.H3,
                    "4" => ETimeFrameTws.H4,
                    "8" => ETimeFrameTws.H8,
                };
                break;

            case "1 day":
                return ETimeFrameTws.D1;

            case "1 week":
                return ETimeFrameTws.W1;


            case "1 month":
                return ETimeFrameTws.MN1;
        }

        throw new ArgumentException();
    }
}