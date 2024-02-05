// created: 2022/06/23 10:53
// author:  rush
// email:   yacper@gmail.com
// 
// purpose:
// modifiers:
// ref: https://interactivebrokers.github.io/tws-api/basic_orders.html

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace NeoTwsApi.Enums;

public enum EOrderTypeTws
{
    //Limit Order 
    [Description("LMT")]
    Limit, 

    /// Peg to market order (Used to be PEG MKT but seems to have been changed, docs don't reflect it).
    [Description("REL")]
    PegToMarket,

    /// <summary>
    /// Market roders
    /// </summary>
    [Description("MKT")]
    Market,

    /// <summary>
    /// Stop loss 止损市价单
    /// </summary>
    [Description("STP")]
    Stop,

    /// <summary>
    /// Stop limit 止损限价单
    /// </summary>
    [Description("STP LMT")]
    StopLimit,
}
