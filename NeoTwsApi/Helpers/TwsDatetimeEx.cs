/********************************************************************
    created:	2022/6/23 9:42:00
    author:		rush
    email:		yacper@gmail.com	
	
    purpose:
    modifiers:	

支持的时区
下面列出的是TWS API支持的时区。

时区	描述
GMT	格林威治标准时间
EST	东部标准时间
MST	山地标准时间
PST	太平洋标准时间
AST	大西洋标准时间
JST	日本标准时间
AET	澳大利亚标准时间
*********************************************************************/
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeoTwsApi.Helpers;

public static class TwsDatetimeEx
{
    public static DateTime UnixTimeZero = DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc);

   public static DateTime ToTwsDateTime(this long mili)
    {// unix mili
        return UnixTimeZero.AddMilliseconds(mili);
    }

    public static DateTime ToTwsDateTime(this string dt)
    {
        //  yyyyMMdd HH:mm:ss {TMZ}
        // 目前发现这两种
        // 20220317  00:00:00
        // 20220317  
        if (dt.Length == 8)
        {
            DateTime ret = DateTime.ParseExact(dt, "yyyyMMdd", CultureInfo.InvariantCulture);
            return ret;
        }
        else
        {
            DateTime ret = DateTime.ParseExact(dt, "yyyyMMdd  HH:mm:ss", CultureInfo.InvariantCulture);

            return ret;
        }
    }

    public static string ToTwsDateTimeString(this DateTime dt)
    {// 20220317  00:00:00
        return dt.ToString("yyyyMMdd  HH:mm:ss");
    }


}