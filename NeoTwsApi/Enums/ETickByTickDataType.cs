// created: 2022/06/23 15:40
// author:  rush
// email:   yacper@gmail.com
// 
// purpose:
// modifiers:
// ref: https://interactivebrokers.github.io/tws-api/tick_data.html

namespace NeoTwsApi.Enums;

public enum ETickByTickDataType
{
    Last,
    AllLast,        //AllLast has additional trade types such as combos, derivatives, and average price trades which are not included in Last. 
    BidAsk,
    MidPoint
}