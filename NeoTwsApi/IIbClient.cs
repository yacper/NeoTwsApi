/********************************************************************
    created:	2022/6/21 1:41:06
    author:		rush
    email:		yacper@gmail.com	
	
    purpose:
    modifiers:	用户服务
*********************************************************************/

using System.ComponentModel;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using NLog;

namespace NeoTwsApi;

public interface IIbClient : INotifyPropertyChanged
{
    /// <summary>
    /// The host of tws
    /// </summary>
    string Host { get; }

    /// <summary>
    /// The port of tws
    /// </summary>
    int Port { get; }

    /// <summary>
    /// Tws client id, see their docs
    /// </summary>
    int ClientId { get; }


    /// <summary>
    /// default timeout duration
    /// </summary>
    int TimeoutMillseconds { get; }

    /// <summary>
    /// Gets a value indicating whether is the client connected to tws
    /// </summary>
    bool Connected { get; }

    /**
         * @brief returns the Host's version. Some of the API functionality might not be available in older Hosts and therefore it is essential to keep the TWS/Gateway as up to date as possible.
         */
    public int ServerVersion { get; }

    public string ServerTime { get; }

    /// <summary>
    /// connect to tws
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<bool> ConnectedAsync();

    /// <summary>
    /// Disconnects the session
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task DisconnectAsync();
}