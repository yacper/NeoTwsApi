// created: 2022/08/12 16:30
// author:  rush
// email:   yacper@gmail.com
// 
// purpose:
// modifiers:

namespace NeoTwsApi.Enums;

public enum ETifTws
{
    Unknown = 0,
    GTC     = 1, // Good-Till-Cancelled
    GTD     = 2, // Good-Till-Date       ib的gtd，实际上是指定日期，必须提供时间，否则报错
    IOC     = 3, // Immediate-or-Cancel
    FOK     = 4, // Fill-or-Kill
    DAY     = 5, // Day         // 当天，一般意义上的GTD
    Minutes = 6, // Order are canceled if not filled in 5 minutes. https://interactivebrokers.github.io/tws-api/cryptocurrency.html
    OPG =7, // Use OPG to send a market-on-open (MOO) or limit-on-open (LOO) order.
}