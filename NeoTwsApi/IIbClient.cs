/********************************************************************
    created:	2022/6/21 1:41:06
    author:		rush
    email:		yacper@gmail.com	
	
    purpose:
    modifiers:	用户服务
*********************************************************************/

using System.Collections.ObjectModel;
using System.ComponentModel;
using AutoFinance.Broker.InteractiveBrokers.Constants;
using IBApi;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using NeoTwsApi.Enums;
using NeoTwsApi.EventArgs;
using NeoTwsApi.Helpers;
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


#region HistoricalData

    Task<List<Bar>> ReqHistoricalDataAsync(Contract contract, DateTime begin, DateTime    end,      ETimeFrameTws tf, EDataType dataType, bool useRth = true);
    Task<List<Bar>> ReqHistoricalDataAsync(Contract contract, DateTime end,   DurationTws duration, ETimeFrameTws tf, EDataType dataType, bool useRth = true);

    #endregion


    #region LiveData ref:https: //interactivebrokers.github.io/tws-api/market_data.html 

    /**
          * @brief Requests tick-by-tick data.\n
          * @param contract - the contract for which tick-by-tick data is requested.\n
          * @param tickType - tick-by-tick data type: "Last", "AllLast", "BidAsk" or "MidPoint".\n
          * @sa EWrapper::tickByTickAllLast, EWrapper::tickByTickBidAsk, EWrapper::tickByTickMidPoint, Contract

            Tick-by-tick data for options is currently only available historically and not in real time.
            Tick-by-tick data for indices is only provided for indices which are on CME.
            Tick-by-tick data is not available for combos.
            No more than 1 tick-by-tick request can be made for the same instrument within 15 seconds.
          */
    Task SubTickByTickData(Contract contract, ETickByTickDataType tickType);

    Task CancelTickByTickData(Contract contract, ETickByTickDataType tickType);

    ReadOnlyObservableCollection<Tuple<Contract, ETickByTickDataType>> TickByTickSubscriptions { get; } 

    event EventHandler<TwsEventArs<Contract, HistoricalTick>> TickByTickMidPointEvent;
    event EventHandler<TwsEventArs<Contract, HistoricalTickLast>> TickByTickLastEvent;
    event EventHandler<TwsEventArs<Contract, HistoricalTickLast>> TickByTickAllLastEvent;
    event EventHandler<TwsEventArs<Contract, HistoricalTickBidAsk>> TickByTickBidAskEvent;




    ///// <summary>
    ///// Request market data from TWS.
    ///// </summary>
    ///// <param name="contract">The contract type</param>
    ///// <param name="genericTickList">The generic tick list</param>
    ///// <param name="snapshot">The snapshot flag</param>
    ///// <param name="regulatorySnapshot">The regulatory snapshot flag</param>
    ///// <param name="mktDataOptions">The market data options</param>
    ///// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    //public Task<TickSnapshotEndEventArgs> RequestMarketDataAsync(
    //    Contract contract,
    //    string genericTickList,
    //    bool snapshot,
    //    bool regulatorySnapshot,
    //    List<TagValue> mktDataOptions)



#endregion

#region Orders

    ///// <summary>
    ///// Send an order to TWS
    ///// </summary>
    ///// <param name="orderId">The order id</param>
    ///// <param name="contract">The contract to trade</param>
    ///// <param name="order">The order parameters</param>
    ///// <returns>True if acknowledged</returns>
    //Task<bool> PlaceOrderAsync(int orderId, Contract contract, Order order);



#endregion
}