// created: 2022/07/21 17:52
// author:  rush
// email:   yacper@gmail.com
// 
// purpose:
// modifiers:

using IBApi;

namespace NeoTwsApi.Helpers;

public static class BarEx
{
    public static DateTime Time(this Bar b)
    {
        return b.Time.ToTwsDateTime();
    }
    
}