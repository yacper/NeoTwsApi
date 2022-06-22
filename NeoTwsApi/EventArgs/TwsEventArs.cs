/********************************************************************
    created:	2022/6/22 11:01:20
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

namespace NeoTwsApi.EventArgs;

public class TwsEventArs<T>
{
    public TwsEventArs(int requestId, T arg)
    {
        this.RequestId = requestId;
        Arg            = arg;
    }

    /// <summary>
    /// Gets the request Id
    /// </summary>
    public int RequestId { get; private set; }


    public T Arg { get; protected set; }
}

public class TwsEventArs<T, T2>
{
    public TwsEventArs(int requestId, T arg, T2 arg2)
    {
        this.RequestId = requestId;
        Arg            = arg;
        Arg2           = arg2;
    }

    /// <summary>
    /// Gets the request Id
    /// </summary>
    public int RequestId { get; private set; }


    public T  Arg  { get; protected set; }
    public T2 Arg2 { get; protected set; }
}