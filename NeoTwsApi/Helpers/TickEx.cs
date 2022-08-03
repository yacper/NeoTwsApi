/********************************************************************
    created:	2022/8/2 17:49:23
    author:		rush
    email:		yacper@gmail.com	
	
    purpose:
    modifiers:	
*********************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IBApi;

namespace NeoTwsApi.Helpers
{
    public static class HistoricalTickBidAskEx
    {
        public static DateTime Time(this HistoricalTickBidAsk t) { return t.Time.ToTwsDateTimeSec(); } // tick 时间精确到second
 
    }
}