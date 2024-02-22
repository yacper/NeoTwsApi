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

    public static DateTime ToTwsDateTimeMili(this long mili, DateTimeKind kind = DateTimeKind.Local)
    {
        var dt = UnixTimeZero.AddMilliseconds(mili);
        return kind == DateTimeKind.Local ? dt.ToLocalTime() : dt.ToUniversalTime();
    }

    public static DateTime ToTwsDateTimeSec(this long seconds, DateTimeKind kind = DateTimeKind.Local)
    {
        var dt = UnixTimeZero.AddSeconds(seconds);
        return kind == DateTimeKind.Local ? dt.ToLocalTime() : dt.ToUniversalTime();
    }



    public static DateTime ToTwsDateTime(this string dt)
    {
        //  yyyyMMdd HH:mm:ss {TMZ}  20220317 00:00:00 Asia/Shanghai | 20220909 09:30:00 US/Eastern
        // 目前发现这两种
        // 20220317  00:00:00
        // 20220317  
        if (dt.Length == 8)     // 代表天的情况，此时，不包含时区信息
        {
            DateTime ret = DateTime.ParseExact(dt, "yyyyMMdd", CultureInfo.InvariantCulture);
            return ret;
        }
        else if (dt.Contains('-'))
        {
            var      ss      = dt.Split(' ');
            var      dstring = ss[0];
            var      tz      = ss[1];   // todo: timezone parse
            DateTime ret     = DateTime.ParseExact(dstring, "yyyyMMdd-HH:mm:ss", CultureInfo.InvariantCulture);

            return ret;
        }
        else
        {
            var      ss      = dt.Split(' ');
            if(ss.Length ==2)
                return DateTime.ParseExact(dt, "yyyyMMdd HH:mm:ss", CultureInfo.InvariantCulture);
            else if (ss.Length == 3)
            {
                var ret = DateTime.ParseExact($"{ss[0]} {ss[1]}", "yyyyMMdd HH:mm:ss", CultureInfo.InvariantCulture);
                var tz  = TimeZoneInfo.FindSystemTimeZoneById(ss[2]);
                return TimeZoneInfo.ConvertTime(ret, tz, TimeZoneInfo.Local);       // 统一转为local
            }

            throw new ArgumentException();
        }
    }

    public static string ToTwsDateTimeString(this DateTime dt)
    {// 20220317  00:00:00
        return dt.ToUniversalTime().ToString("yyyyMMdd-HH:mm:ss");      // 作为utc datetime表示
        //return $"{dt.ToString("yyyyMMdd HH:mm:ss")} Asia/Shanghai";
    }


}