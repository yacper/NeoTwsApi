// created: 2022/07/21 17:52
// author:  rush
// email:   yacper@gmail.com
// 
// purpose:
// modifiers:

using FluentDateTime;
using IBApi;
using NeoTwsApi.Enums;

namespace NeoTwsApi.Helpers;

public static class BarEx
{
    public static DateTime Time(this Bar b)
    {
        var t= b.Time.ToTwsDateTime();
        return t;
    }

    // 正规化bars
    public static List<Bar> ModBars(this List<Bar> bars, ETimeFrameTws tf)
    {
        var ret = new List<Bar>();
        switch (tf)
        {
            case ETimeFrameTws.W1:
            {
                // 周线数据序列，通过ClientSocket_.reqHistoricalData获取的时候，time是周五的日期，另外，最近一根是当前最新的日期，所以需要调整
                // 将所有的日期调整为周一
                foreach (var b in bars)
                {
                    ret.Add(new Bar(
                                    b.Time.ToTwsDateTime().FirstDayOfWeek().ToString("yyyyMMdd"),
                            b.Open,
                            b.High,
                            b.Low,
                            b.Close,
                            b.Volume,
                            b.Count,
                            b.WAP
                           ));
                }

                return ret;
            }
                break;
            case ETimeFrameTws.MN1:
            {
                // 月线数据序列，通过ClientSocket_.reqHistoricalData获取的时候，time是月末的日期，另外，最近一根是当前最新的日期，所以需要调整
                // 将所有的日期调整为周一
                foreach (var b in bars)
                {
                    ret.Add(new Bar(
                                    b.Time.ToTwsDateTime().FirstDayOfMonth().ToString("yyyyMMdd"),
                            b.Open,
                            b.High,
                            b.Low,
                            b.Close,
                            b.Volume,
                            b.Count,
                            b.WAP
                           ));
                }

                return ret;
            }
                break;
            
        }

        return bars;
    }
}