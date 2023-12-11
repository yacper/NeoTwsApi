/********************************************************************
    created:	2022/6/21 1:41:06
    author:		rush
    email:		yacper@gmail.com	
	
    purpose:
    modifiers:	Ib Api 
*********************************************************************/

using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using IBApi;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using NeoTwsApi.Enums;
using NeoTwsApi.EventArgs;
using NeoTwsApi.Helpers;
using NeoTwsApi.Models;
using NLog;

namespace NeoTwsApi;
public enum EConnectionState
{
    Disconnected =0,
    Connecting,
    Connected,
    Disconnecting,
}

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
    int TimeoutMilliseconds { get; set; } 

    /**
         * @brief returns the Host's version. Some of the API functionality might not be available in older Hosts and therefore it is essential to keep the TWS/Gateway as up to date as possible.
         */
    public int ServerVersion { get; }

    public string ConnectedServerTime { get; }  // Server time when connected, some string can't recognize


    public event EventHandler<NeoTwsApi.EventArgs.ErrorEventArgs> ErrorEvent;

#region Login

    /// <summary>
    /// Gets a value indicating whether is the client connected to tws
    /// </summary>
    EConnectionState ConnectionState { get; }


    /// <summary>
    /// connect to tws
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<bool> ConnectAsync(string host = null, int? port = null, int? clientId = null);

    /// <summary>
    /// Disconnects the session
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task DisconnectAsync();

#endregion

#region Account

    ReadOnlyObservableCollection<string>            Accounts { get; }
    Task<AccountDetails>                            ReqAccountDetailsAsync(string accountId); // get account details
    void                                            CancelAccountDetails(string accountId);
    event EventHandler<UpdateAccountValueEventArgs> UpdateAccountValueEvent;

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
    /// <summary>
    /// Request historical data from TWS
    /// </summary>
    /// <param name="contract">The contract</param>
    /// <param name="end">The end time</param>
    /// <param name="duration">The duration string</param>
    /// <param name="dataType">The things to show (?)</param>
    /// <param name="useRth">Whether to use regular trading hours</param>
    Task<List<Bar>> ReqHistoricalDataAsync(Contract contract, DateTime end, DurationTws duration, ETimeFrameTws tf, EDataType dataType, bool useRth = true);

    // 模拟而得
    Task<List<Bar>> ReqHistoricalDataAsync(Contract contract, DateTime start, DateTime end, ETimeFrameTws tf, EDataType dataType, bool useRth = true);
#endregion


#region Stream data ref:https: //interactivebrokers.github.io/tws-api/market_data.html 

    // 虽然tws还提供了reqMktData用于获取contract的价格等各类信息，但都延时或不实用
    // 实际上，真正有用的stream data，只有TickByTickData

    /**
          * @brief Requests tick-by-tick data.\n
          * @param contract - the contract for which tick-by-tick data is requested.\n
          * @param tickType - tick-by-tick data type: "Last", "AllLast", "BidAsk" or "MidPoint".\n
          * @sa EWrapper::tickByTickAllLast, EWrapper::tickByTickBidAsk, EWrapper::tickByTickMidPoint, Contract

            Tick-by-tick data for options is currently only available historically and not in real time.
            Tick-by-tick data for indices is only provided for indices which are on CME.
            Tick-by-tick data is not available for combos.
            No more than 1 tick-by-tick request can be made for the same instrument within 15 seconds.

            xauusd不支持，也许commodity都不支持
            Failed to request tick-by-tick data:BidAsk tick-by-tick requests are not supported for XAUUSD

            https://interactivebrokers.github.io/tws-api/market_data.html
          */
    Task SubTickByTickDataAsync(Contract contract, ETickByTickDataType tickType);

    // 取消订阅，但并不能保证成功，即便取消了，也可能还会收到数据
    void UnsubTickByTickData(Contract contract, ETickByTickDataType tickType);

    ReadOnlyObservableCollection<Tuple<Contract, ETickByTickDataType>> TickByTickSubscriptions { get; }

    event EventHandler<TwsEventArs<Contract, HistoricalTick>>       TickByTickMidPointEvent;
    event EventHandler<TwsEventArs<Contract, HistoricalTickLast>>   TickByTickLastEvent;
    event EventHandler<TwsEventArs<Contract, HistoricalTickLast>>   TickByTickAllLastEvent;
    event EventHandler<TwsEventArs<Contract, HistoricalTickBidAsk>> TickByTickBidAskEvent;

    /// <summary>
    /// Request 5 Second Real Time Bars data from TWS
    /// Caution: Not really real time!
    /// </summary>
    /// <param name="contract">The contract</param>
    /// <param name="whatToShow">The things to show (?)</param>
    /// <param name="useRTH">Whether to use regular trading hours</param>
    /// <param name="realtimeBarOptions">The realtime bar options</param>
    Task SubRealtimeBarsAsync(Contract contract, EDataType datType, bool useRTH = true);

    void UnsubRealtimeBars(Contract contract);

    ReadOnlyObservableCollection<Contract> RealtimeBarsSubscriptions { get; }

    event EventHandler<TwsEventArs<Contract, Bar>> RealtimeBarEvent; // 5 sec bar

#endregion

#region Orders

    /// <summary>
    /// Send an order to TWS
    /// </summary>
    /// <param name="contract">The contract to trade</param>
    /// <param name="order">The order parameters</param>
    /// <returns>True if acknowledged</returns>
    Task<OpenOrderEventArgs> PlaceOrderAsync(Contract contract, Order order);

    event EventHandler<OrderStatusEventArgs> OrderStatusEvent;  // 订单状态变化时间
    event EventHandler<OpenOrderEventArgs> OpenOrderEvent;  // 新订单事件 当订单状态发生变化时，也会调用该事件（该事件在新开订单时，可能会被调用6,7次）
    /// 订单成交后，将触发ExecutionDetailsEvent&CommissionReportEvent 这2个事件
    event EventHandler<ExecutionDetailsEventArgs> ExecutionDetailsEvent;
    event EventHandler<CommissionReport> CommissionReportEvent;


    /// <summary>
    /// Requests open orders
    /// </summary>
    /// <returns>Open orders</returns>
    Task<List<OpenOrderEventArgs>> ReqOpenOrdersAsync();

    Task<List<CompletedOrderEventArgs>> ReqCompletedOrdersAsync();

    /// <summary>
    /// Cancels an order
    /// </summary>
    /// <param name="orderId">The order ID</param>
    /// <returns>True if it was successfully cancelled</returns>
    Task<bool> CancelOrderAsync(int orderId);

#endregion

#region Positions

    /// <summary>
    /// Get a list of all the positions in TWS.
    //Also, subscribe the status changes of the positions
    /// </summary>
    /// <returns>A list of position status events from TWS.</returns>
    Task<List<PositionStatusEventArgs>> ReqPositions();

    event EventHandler<PositionStatusEventArgs> PositionStatusEvent;

    /// <summary>
    /// Sends a message to TWS telling it to stop sending position information through the socket.
    /// </summary>
    void UnsubPositions();

#endregion
}