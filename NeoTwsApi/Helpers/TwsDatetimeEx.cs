/********************************************************************
    created:	2022/6/23 9:42:00
    author:		rush
    email:		yacper@gmail.com	
	
    purpose:
    modifiers:	
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
    public static DateTime ToTwsDateTime(this string dt)
    {// 20220317  00:00:00
        DateTime ret = DateTime.ParseExact(dt, "yyyyMMdd  HH:mm:ss", CultureInfo.InvariantCulture);

        return ret;
    }

    public static string ToTwsDateTimeString(this DateTime dt)
    {// 20220317  00:00:00
        return dt.ToString("yyyyMMdd  HH:mm:ss");
    }


}