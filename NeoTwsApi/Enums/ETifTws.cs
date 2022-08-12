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
    GTD     = 2, // Good-Till-Day
    IOC     = 3, // Immediate-or-Cancel
    FOK     = 4, // Fill-or-Kill
    DAY     = 5, // Day
}