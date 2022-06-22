/********************************************************************
    created:	2022/6/21 1:41:06
    author:		rush
    email:		yacper@gmail.com	
	
    purpose:
    modifiers:	用户服务
*********************************************************************/

using System.Collections.ObjectModel;
using System.ComponentModel;
using IBApi;
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
    int TimeoutMilliseconds { get; }

   
    /**
         * @brief returns the Host's version. Some of the API functionality might not be available in older Hosts and therefore it is essential to keep the TWS/Gateway as up to date as possible.
         */
    public int ServerVersion { get; }

    public string ServerTime { get; }

    #region Account
    IReadOnlyList<string> Accounts { get; }

    

    #endregion

    #region Login

    

    /// <summary>
    /// Gets a value indicating whether is the client connected to tws
    /// </summary>
    bool Connected { get; }


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

    #endregion

    #region Contract

    /// <summary>
    /// Gets a contract by request.
    /// </summary>
    /// <param name="contract">The requested contract.</param>
    /// <returns>The details of the contract</returns>
    Task<List<ContractDetails>> ReqContractAsync(Contract contract);

   /**
        * @brief Requests matching stock symbols
        * @param pattern - either start of ticker symbol or (for larger strings) company name
        */
    Task<List<ContractDescription>> ReqMatchingSymbolsAsync(string pattern);

    #endregion
}