// created: 2023/12/08 17:58
// author:  rush
// email:   yacper@gmail.com
// 
// purpose:
// modifiers:

using IBApi;

namespace NeoTwsApi.Helpers;

public static class ApiHelper
{
    // 简单比较contract是否相同
    public static bool EqualSimple(this Contract left, Contract right)
    {
        if (left == null && right == null)
        {
            return true;
        }
        if (left == null || right == null)
        {
            return false;
        }

        return left.LocalSymbol == right.LocalSymbol &&
               left.SecType == right.SecType &&
               left.Exchange == right.Exchange &&
               left.Currency == right.Currency;
    }

    
}