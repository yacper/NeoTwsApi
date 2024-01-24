// created: 2022/06/28 15:56
// author:  rush
// email:   yacper@gmail.com
// 
// purpose:
// modifiers:

namespace NeoTwsApi.Enums;

public enum ESecTypeTws
{
    Unknown,
    STK,   // A stock contract
    OPT,   // An option contract
    FUT,   // A future contract
    IND,   // An index contract
    FOP,   // A future option contract
    CASH,  // A forex pair contract
    BAG,   // A combo contract
    WAR,   // A warrant contract
    BOND,  // A bond contract
    CMDTY, // A commodity contract
    FUND,  // A mutual fund contract
    NEWS,  // A news contract
    CFD,    // 
    CRYPTO, //
}