// created: 2022/06/22 17:41
// author:  rush
// email:   yacper@gmail.com
// 
// purpose:
// modifiers:
// ref:https://interactivebrokers.github.io/tws-api/historical_bars.html#hd_what_to_show 

namespace NeoTwsApi.Enums;

public enum EDataType
{
    TRADES,
    MIDPOINT,
    BID,
    ASK,
    BID_,
    ADJUSTED_LAST,
    HISTORICAL_VOLATILITY,
    OPTION_IMPLIED_VOLATILITY,
    REBATE_RATE,
    FEE_RATE,
    YIELD_BID,
    YIELD_ASK,
    YIELD_BID_ASK,
    YIELD_LAST,
    SCHEDULE
}