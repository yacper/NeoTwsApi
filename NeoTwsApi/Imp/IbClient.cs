﻿/********************************************************************
    created:	2022/6/21 1:41:06
    author:		rush
    email:		yacper@gmail.com	
	
    purpose:
    modifiers:	用户服务
*********************************************************************/

using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using IBApi;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using NLog;
using Microsoft.VisualBasic.CompilerServices;
using MoreLinq;
using NeoTwsApi.Enums;
using NeoTwsApi.EventArgs;
using NeoTwsApi.Exceptions;
using NeoTwsApi.Helpers;
using NeoTwsApi.Imp;
using ErrorEventArgs = NeoTwsApi.EventArgs.ErrorEventArgs;
using System.Globalization;
using FluentDateTime;
using MoreLinq.Extensions;
using NeoTwsApi.Constants;
using NeoTwsApi.Models;

namespace NeoTwsApi;

public class IbClient : ObservableObject, IIbClient
{
    public string Host { get; protected set; }

    public int Port { get; protected set; }

    public int ClientId { get; protected set; }

    public override string ToString() => $"IbClient[{Host}:{Port}-{ClientId}]";

    public int TimeoutMilliseconds { get; set; } = 10000;

    public ILogger? Logger { get; protected set; }


    public EConnectionState ConnectionState
    {
        get => ConnectionState_;
        protected set
        {
            if (value != ConnectionState)
                Logger?.Info($"{this.ToString()} {value} ");

            SetProperty(ref ConnectionState_, value);
        }
    }

    public int ServerVersion { get; protected set; } = 0;

    public string ConnectedServerTime { get; protected set; } = null;


    public event EventHandler<NeoTwsApi.EventArgs.ErrorEventArgs> ErrorEvent;

    public ReadOnlyObservableCollection<string> Accounts { get; set; }

    public event EventHandler<UpdateAccountValueEventArgs> UpdateAccountValueEvent;


    public IbClient(string host, int port, int clientId, ILogger? logger = null)
    {
        Host     = host;
        Port     = port;
        ClientId = clientId;
        Logger   = logger;

        Accounts = new(Accounts_);

        TwsCallbackHandler_ = new TwsCallbackHandler(this);

        AddHandler_();
    }

    protected void AddHandler_()
    {
        TwsCallbackHandler_.ErrorEvent += TwsCallbackHandler__ErrorEvent;
        TwsCallbackHandler_.TickByTickBidAskEvent   += TwsCallbackHandler_TickByTickBidAskEvent;
        TwsCallbackHandler_.TickByTickMidPointEvent += TwsCallbackHandler_TickByTickMidPointEvent;
        TwsCallbackHandler_.TickByTickLastEvent     += TwsCallbackHandler_TickByTickLastEvent;
        TwsCallbackHandler_.TickByTickAllLastEvent  += TwsCallbackHandlerTickByTickAllLastEvent;

        TwsCallbackHandler_.RealtimeBarEvent += TwsCallbackHandler_RealtimeBarEvent;
        //ClientSocket_   = new TwsClientSocket(_EClientSocket);
        //ClientSocket_   = _EClientSocket;

        TwsCallbackHandler_.UpdateAccountValueEvent += _OnUpdateAccountValueEvent;

        TwsCallbackHandler_.OrderStatusEvent      += TwsCallbackHandler__OrderStatusEvent;
        TwsCallbackHandler_.OpenOrderEvent        += TwsCallbackHandler__OpenOrderEvent;
        TwsCallbackHandler_.ExecutionDetailsEvent += TwsCallbackHandler_ExecutionDetailsEvent;
        TwsCallbackHandler_.CommissionReportEvent += TwsCallbackHandler_CommissionReportEvent;

        TwsCallbackHandler_.PositionStatusEvent += TwsCallbackHandler_PositionStatusEvent;
    }



    protected void RemoveHandler_()
    {
        TwsCallbackHandler_.TickByTickBidAskEvent   -= TwsCallbackHandler_TickByTickBidAskEvent;
        TwsCallbackHandler_.TickByTickMidPointEvent -= TwsCallbackHandler_TickByTickMidPointEvent;
        TwsCallbackHandler_.TickByTickLastEvent     -= TwsCallbackHandler_TickByTickLastEvent;
        TwsCallbackHandler_.TickByTickAllLastEvent  -= TwsCallbackHandlerTickByTickAllLastEvent;

        TwsCallbackHandler_.RealtimeBarEvent -= TwsCallbackHandler_RealtimeBarEvent;
        //ClientSocket_   = new TwsClientSocket(_EClientSocket);
        //ClientSocket_   = _EClientSocket;

        TwsCallbackHandler_.UpdateAccountValueEvent -= _OnUpdateAccountValueEvent;

        TwsCallbackHandler_.ExecutionDetailsEvent -= TwsCallbackHandler_ExecutionDetailsEvent;
        TwsCallbackHandler_.CommissionReportEvent -= TwsCallbackHandler_CommissionReportEvent;

        TwsCallbackHandler_.PositionStatusEvent -= TwsCallbackHandler_PositionStatusEvent;
    }

    private void TwsCallbackHandler__ErrorEvent(object? sender, ErrorEventArgs e)
    {
        ErrorEvent?.Invoke(this, e);
    }

    private void TwsCallbackHandler__OpenOrderEvent(object? sender, OpenOrderEventArgs e) { this.OpenOrderEvent?.Invoke(this, e); }

    private void TwsCallbackHandler__OrderStatusEvent(object? sender, OrderStatusEventArgs e) { this.OrderStatusEvent?.Invoke(this, e); }


    private void TwsCallbackHandler_PositionStatusEvent(object? sender, PositionStatusEventArgs e) { this.PositionStatusEvent?.Invoke(this, e); }

    private void TwsCallbackHandler_CommissionReportEvent(object? sender, CommissionReportEventArgs e) { this.CommissionReportEvent?.Invoke(this, e.CommissionReport); }

    private void TwsCallbackHandler_ExecutionDetailsEvent(object? sender, ExecutionDetailsEventArgs e) { this.ExecutionDetailsEvent?.Invoke(this, e); }


    private void TwsCallbackHandlerTickByTickAllLastEvent(object? sender, TwsEventArs<Contract, HistoricalTickLast> e) { TickByTickAllLastEvent?.Invoke(this, e); }

    private void TwsCallbackHandler_TickByTickLastEvent(object? sender, TwsEventArs<Contract, HistoricalTickLast> e) { TickByTickLastEvent?.Invoke(this, e); }

    private void TwsCallbackHandler_TickByTickMidPointEvent(object? sender, TwsEventArs<Contract, HistoricalTick> e) { TickByTickMidPointEvent?.Invoke(this, e); }

    private void TwsCallbackHandler_TickByTickBidAskEvent(object? sender, TwsEventArs<Contract, HistoricalTickBidAsk> e) { TickByTickBidAskEvent?.Invoke(this, e); }


    private void TwsCallbackHandler_RealtimeBarEvent(object? sender, TwsEventArs<Contract, Bar> e) { RealtimeBarEvent?.Invoke(this, e); }

    public async Task<bool> ConnectAsync(string host = null, int? port = null, int? clientId = null)
    {
        if (ConnectionState == EConnectionState.Disconnected)
        {
            await this.ConnectAsync_(host, port, clientId);

            //// Sometimes TWS flushes the socket on a new connection
            //// And the socket will get really fucked up any commands come in during that time
            //// Just wait 5 seconds for it to finish
            //await Task.Delay(5000);
        }

        return ConnectionState == EConnectionState.Connected;
    }


    /// <summary>
    /// Connect to the TWS socket and launch a background thread to begin firing the events.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    private Task ConnectAsync_(string host = null, int? port = null, int? clientId = null)
    {
        if (host != null) Host         = host;
        if (port != null) Port         = port.Value;
        if (clientId != null) ClientId = clientId.Value;

        ConnectionState = EConnectionState.Connecting;

        Signal_             = new EReaderMonitorSignal();
        ClientSocket_       = new EClientSocket(TwsCallbackHandler_, Signal_);

        var                                taskSource                        = new TaskCompletionSource<bool>();
        EventHandler<NextValidIdEventArgs> nextValidIdEventHandler           = null;
        EventHandler                       connectionAcknowledgementCallback = null;

        void clearHandler()
        {
            this.TwsCallbackHandler_.ConnectionAcknowledgementEvent -= connectionAcknowledgementCallback;
            this.TwsCallbackHandler_.NextValidIdEvent               -= nextValidIdEventHandler;
        }

        connectionAcknowledgementCallback = (s, e) =>
        {
            this.ThreadRunning_ = true;

            // When the connection is acknowledged, create a reader to consume messages from the TWS.
            // The EReader will consume the incoming messages and the callback handler will begin to fire events.
            this.TwsCallbackHandler_.ConnectionAcknowledgementEvent -= connectionAcknowledgementCallback;
            var reader = new EReader(this.ClientSocket_, this.Signal_);
            reader.Start();

            this.ReaderThread_ = new Thread(
                                            () =>
                                            {
                                                while (ThreadRunning_)
                                                {
                                                    this.Signal_.waitForSignal();
                                                    lock (ReaderLock_)
                                                    {
                                                        if(ThreadRunning_)
                                                            reader.processMsgs();
                                                    }
                                                }
                                            })
                { IsBackground = true };
            this.ReaderThread_.Start();
        };

        nextValidIdEventHandler = (s, e) =>
        {
            lock (Lock_)
            {
                if (ConnectionState == EConnectionState.Disconnected)
                    return;

                clearHandler();
                //nextValidId() indicate really connected
                OnConnected(e.OrderId);

                taskSource.TrySetResult(true);
            }
        };

        // Set the operation to cancel after 5 seconds
        CancellationTokenSource tokenSource = new CancellationTokenSource(TimeoutMilliseconds);
        tokenSource.Token.Register(() =>
        {
            lock (Lock_)
            {
                /// 超时失败
                clearHandler();

                if (ConnectionState == EConnectionState.Connecting)
                {
                    this.ClientSocket_.eDisconnect();
                    this.ThreadRunning_ = false;
                    ConnectionState     = EConnectionState.Disconnected;
                }

                taskSource.TrySetCanceled();
            }
        });

        this.TwsCallbackHandler_.ConnectionAcknowledgementEvent += connectionAcknowledgementCallback;
        this.TwsCallbackHandler_.NextValidIdEvent               += nextValidIdEventHandler;

        this.ClientSocket_.eConnect(this.Host, this.Port, this.ClientId);
        return taskSource.Task;
    }

    public Task DisconnectAsync()
    {
        if (ConnectionState != EConnectionState.Connected)
            return Task.CompletedTask;

        ConnectionState = EConnectionState.Disconnecting;

        var          taskSource              = new TaskCompletionSource();
        EventHandler connectionClosedHandler = null;
        connectionClosedHandler = (s, e) =>
        {
            lock (Lock_)
            {
                this.TwsCallbackHandler_.ConnectionClosedEvent -= connectionClosedHandler;

                // Abort the reader thread
                ThreadRunning_ = false;

                OnDisconnected();
                taskSource.TrySetResult();
            }

        };
        this.TwsCallbackHandler_.ConnectionClosedEvent += connectionClosedHandler;

        // Set the operation to cancel after 5 seconds
        CancellationTokenSource tokenSource = new CancellationTokenSource(TimeoutMilliseconds);
        tokenSource.Token.Register(() =>
        {
            lock (Lock_)
            {
                this.TwsCallbackHandler_.ConnectionClosedEvent -= connectionClosedHandler;
                taskSource.TrySetCanceled();
            }
        });

        // 意外中断事件删除
        this.TwsCallbackHandler_.ConnectionClosedEvent -= TwsCallbackHandler_OnConnectionClosedEvent;

        ThreadRunning_ = false;
        lock (ReaderLock_)
        {
            this.ClientSocket_.eDisconnect();
            this.ClientSocket_ = null;
        }

        return taskSource.Task;
    }

    protected void OnConnected(int nextValidId = 0)
    {
        Interlocked.Exchange(ref NextValidOrderId_, nextValidId);

        TwsRequestIdGenerator_ = new TwsRequestIdGenerator();

        ServerVersion = ClientSocket_.ServerVersion;
        //DateTime ret = DateTime.ParseExact(ClientSocket_.ConnectedServerTime, "yyyyMMdd  HH:mm:ss zzz", CultureInfo.InvariantCulture);
        ConnectedServerTime = ClientSocket_.ServerTime;

        ConnectionState = EConnectionState.Connected;

        // 中途断线
        this.TwsCallbackHandler_.ConnectionClosedEvent += TwsCallbackHandler_OnConnectionClosedEvent;
    }

    private void TwsCallbackHandler_OnConnectionClosedEvent(object? sender, System.EventArgs e)
    {
        this.TwsCallbackHandler_.ConnectionClosedEvent -= TwsCallbackHandler_OnConnectionClosedEvent;
        Logger?.Error("IB 连接中断");

        // Abort the reader thread
        ThreadRunning_ = false;

        OnDisconnected();
    }

    public void OnAccountsReceived(string accountsList) // 当连接建立后，tws后会主动发送managedAccounts
    {
        Accounts_.Clear();
        MoreEnumerable.ForEach(accountsList.Split(','), p => Accounts_.Add(p));
    }

    protected void OnDisconnected()
    {
        RealtimeBarsSubscriptions_.Clear();
        RealtimeBarsSubscriptionReqs_.Clear();
        TickByTickSubscriptionReqs_.Clear();
        TickByTickSubscriptions_.Clear();
        ReqContracts.Clear();

        ConnectionState = EConnectionState.Disconnected;
    }


#region Account

    public Task<AccountDetails> ReqAccountDetailsAsync(string accountId)
    {
        var taskSource = new TaskCompletionSource<AccountDetails>();

        EventHandler<AccountDownloadEndEventArgs> accountDownloadEndHandler = null;
        accountDownloadEndHandler = (s, e) =>
        {
            this.TwsCallbackHandler_.AccountDownloadEndEvent -= accountDownloadEndHandler; // always clear
            taskSource.TrySetResult(this.AccountUpdates_[accountId]);
        };

        this.TwsCallbackHandler_.AccountDownloadEndEvent += accountDownloadEndHandler;

        CancellationTokenSource tokenSource = new CancellationTokenSource(TimeoutMilliseconds);
        tokenSource.Token.Register(() =>
        {
            this.TwsCallbackHandler_.AccountDownloadEndEvent -= accountDownloadEndHandler; // always clear
            taskSource.TrySetCanceled();
        });

        this.ClientSocket_.reqAccountUpdates(true, accountId);

        return taskSource.Task;
    }


    /// <summary>
    /// Run when an account value is updated
    /// </summary>
    /// <param name="sender">The sender</param>
    /// <param name="eventArgs">The event arguments</param>
    private void _OnUpdateAccountValueEvent(object sender, UpdateAccountValueEventArgs eventArgs)
    {
        if (!AccountUpdates_.ContainsKey(eventArgs.AccountName))
            AccountUpdates_[eventArgs.AccountName] = new AccountDetails();

        this.AccountUpdates_[eventArgs.AccountName].AddOrUpdate(eventArgs.Key, eventArgs.Value, (key, value) =>
        {
            return eventArgs.Value; // Always take the most recent result
        });

        UpdateAccountValueEvent?.Invoke(this, eventArgs);
    }

    public void CancelAccountDetails(string accountId) { this.ClientSocket_.reqAccountUpdates(false, accountId); }

    /// <summary>
    /// The account updates dictionary
    /// </summary>
    private Dictionary<string, AccountDetails> AccountUpdates_ = new();

#endregion


#region contract

    /// <summary>
    /// Gets a contract by request.
    /// </summary>
    /// <param name="contract">The requested contract.</param>
    /// <returns>The details of the contract</returns>
    public Task<List<ContractDetails>> ReqContractAsync(Contract contract)
    {
        int                   requestId           = this.TwsRequestIdGenerator_.GetNextRequestId();
        List<ContractDetails> contractDetailsList = new List<ContractDetails>();

        var taskSource = new TaskCompletionSource<List<ContractDetails>>();

        EventHandler<ContractDetailsEventArgs>    contractDetailsEventHandler    = null;
        EventHandler<ContractDetailsEndEventArgs> contractDetailsEndEventHandler = null;
        EventHandler<ErrorEventArgs>              errorEventHandler              = null;

        contractDetailsEventHandler = (sender, args) =>
        {
            if (args.RequestId == requestId)
            {
                // When the contract details end event is fired, check if it's for this request ID and return it.
                contractDetailsList.Add(args.ContractDetails);
            }
        };

        contractDetailsEndEventHandler += (sender, args) =>
        {
            if (args.RequestId == requestId)
            {
                /// clean handler
                this.TwsCallbackHandler_.ContractDetailsEvent -=
                    contractDetailsEventHandler;
                this.TwsCallbackHandler_.ContractDetailsEndEvent -=
                    contractDetailsEndEventHandler;
                this.TwsCallbackHandler_.ErrorEvent -= errorEventHandler;

                taskSource.TrySetResult(contractDetailsList);
            }
        };

        errorEventHandler = (sender, args) =>
        {
            if (args.Id == requestId)
            {
                // The error is associated with this request
                this.TwsCallbackHandler_.ContractDetailsEvent    -= contractDetailsEventHandler;
                this.TwsCallbackHandler_.ContractDetailsEndEvent -= contractDetailsEndEventHandler;
                this.TwsCallbackHandler_.ErrorEvent              -= errorEventHandler;

                taskSource.TrySetException(new TwsException(args));
            }
        };

        // Set the operation to cancel after 5 seconds
        CancellationTokenSource tokenSource = new CancellationTokenSource(TimeoutMilliseconds);
        tokenSource.Token.Register(() => { taskSource.TrySetCanceled(); });

        this.TwsCallbackHandler_.ContractDetailsEvent    += contractDetailsEventHandler;
        this.TwsCallbackHandler_.ContractDetailsEndEvent += contractDetailsEndEventHandler;
        this.TwsCallbackHandler_.ErrorEvent              += errorEventHandler;

        this.ClientSocket_.reqContractDetails(requestId, contract);
        return taskSource.Task;
    }


    public Task<List<ContractDescription>> ReqMatchingSymbolsAsync(string pattern)
    {
        int                       requestId           = this.TwsRequestIdGenerator_.GetNextRequestId();
        List<ContractDescription> contractDetailsList = new List<ContractDescription>();

        var taskSource = new TaskCompletionSource<List<ContractDescription>>();

        EventHandler<TwsEventArs<ContractDescription[]>> symbolSamplesHandler = null;

        symbolSamplesHandler = (sender, args) =>
        {
            if (args.RequestId == requestId)
            {
                contractDetailsList.AddRange(args.Arg);

                this.TwsCallbackHandler_.SymbolSamplesEvent -= symbolSamplesHandler;
                taskSource.TrySetResult(contractDetailsList);
            }
        };

        // Set the operation to cancel after 5 seconds
        CancellationTokenSource tokenSource = new CancellationTokenSource(TimeoutMilliseconds);
        tokenSource.Token.Register(() => { taskSource.TrySetCanceled(); });

        this.TwsCallbackHandler_.SymbolSamplesEvent += symbolSamplesHandler;

        ClientSocket_.reqMatchingSymbols(requestId, pattern);
        return taskSource.Task;
    }

#endregion

#region HistoricalData

    //public Task<List<Bar>> ReqHistoricalDataAsync(Contract contract, DateTime begin, DateTime end, ETimeFrameTws tf, EDataType dataType, bool useRth = true)
    //{
    //    // covert [begin. end] to duration
    //    throw new NotImplementedException();
    //}

    public Task<List<Bar>> ReqHistoricalDataAsync(Contract contract, DateTime end, DurationTws duration, ETimeFrameTws tf, EDataType dataType, bool useRth = true, int formatDate=1, bool keepUpToDate=false, List<TagValue> chartOptions=null)
    {
        int requestId = this.TwsRequestIdGenerator_.GetNextRequestId();
        ReqContracts[requestId] = new(contract, null);

        var taskSource = new TaskCompletionSource<List<Bar>>();

        EventHandler<TwsEventArs<Bar>>                historicalDataEventHandler    = null;
        EventHandler<TwsEventArs<DateTime, DateTime>> historicalDataEndEventHandler = null;
        EventHandler<ErrorEventArgs>                  errorEventHandler             = null;

        void clearHandler()
        {
            this.TwsCallbackHandler_.HistoricalDataEvent    -= historicalDataEventHandler;
            this.TwsCallbackHandler_.HistoricalDataEndEvent -= historicalDataEndEventHandler;
            this.TwsCallbackHandler_.ErrorEvent             -= errorEventHandler;
        }

        List<Bar> historicalDataList = new();

        historicalDataEventHandler = (sender, args) =>
        {
            if (args.RequestId == requestId) { historicalDataList.Add(args.Arg); }
        };

        historicalDataEndEventHandler = (sender, args) =>
        {
            if (args.RequestId == requestId)
            {
                clearHandler();
                taskSource.TrySetResult(historicalDataList);
            }
        };

        errorEventHandler = (sender, args) =>
        {
            if (args.Id == requestId)
            {
                //todo:warn
                //this.CancelHistoricalData(requestId);

                // The error is associated with this request
                clearHandler();
                taskSource.TrySetException(new TwsException(args));
            }
        };

        // Set the operation to cancel after 2 minute
        CancellationTokenSource tokenSource = new CancellationTokenSource(TimeoutMilliseconds);
        tokenSource.Token.Register(() =>
        {
            //todo:warn
            //this.CancelHistoricalData(requestId);
            clearHandler();

            taskSource.TrySetCanceled();
        });

        this.TwsCallbackHandler_.HistoricalDataEvent    += historicalDataEventHandler;
        this.TwsCallbackHandler_.HistoricalDataEndEvent += historicalDataEndEventHandler;
        this.TwsCallbackHandler_.ErrorEvent             += errorEventHandler;


        this.ClientSocket_.reqHistoricalData(
                                             requestId,
                                             contract,
                                             end.ToTwsDateTimeString(),
                                             duration.ToString(),
                                             tf.ToTwsString(),
                                             dataType.ToString(),
                                             useRth ? 1 : 0,
                                             formatDate,     // default format date
                                             keepUpToDate, // not keep update
                                             chartOptions);

        return taskSource.Task;
    }


    public async Task<List<Bar>> ReqHistoricalDataAsync2(Contract contract, DateTime start, DateTime end, ETimeFrameTws tf, EDataType dataType, bool useRth = true)
    {
        if (contract == null)
            throw new ArgumentNullException(nameof(contract));

        if (start > end)
            throw new ArgumentException($"start({start}) > end({end})");

        //获取XAUUSD历史数据失败:Error validating request:-'bX' : cause - Historical data requests for durations longer than 365 days must be made in years.
        // 如果时间跨度超过365天，需要分段请求
        // 1. 先请求最后365天的数据
        // 2. 然后再请求剩余的数据
        // 3. 合并数据
        // 4. 返回
        // 5. 如果有多余，截掉多余
        if (tf <= ETimeFrameTws.D1 && end - start > TimeSpan.FromDays(365))
        {
            var ret = await ReqHistoricalDataAsync2(contract, end - TimeSpan.FromDays(365), end, tf, dataType, useRth);
            var ret2 = await ReqHistoricalDataAsync2(contract, start, end - TimeSpan.FromDays(365), tf, dataType, useRth);
            ret2.AddRange(ret);
            return ret2;
        }

        // 如果超过52周，必须用year， Error validating request:-'bX' : cause - Historical data request for durations longer than 52 weeks must be made in years.”
        else
        {
            var ret = await ReqHistoricalDataAsync(contract, end, DurationTwsEx.ToDurationTws(start, end, tf), tf, dataType, useRth);

            // 如果需要mod日期信息
            ret = ret.ModBars(tf);

            // 如果有多余，截掉多余
            var clampStart = start;
            switch (tf)
            {
                case ETimeFrameTws.W1:
                    clampStart = start.FirstDayOfWeek().Date;
                    break;
                case ETimeFrameTws.MN1:
                    clampStart = start.FirstDayOfMonth().Date;
                    break;
            }

            int index = ret.FindIndex(p => p.Time() >= clampStart);
            if(index != -1)
                ret.RemoveRange(0, index);
            return ret;
        }
    }

#endregion


#region Orders

    /// <summary>
    /// Places an order and returns whether the order placement was successful or not.
    /// </summary>
    /// <param name="orderId">The order Id</param>
    /// <param name="contract">The contract to trade</param>
    /// <param name="order">The order</param>
    /// <param name="cancellationToken">The cancellation token used to cancel the request</param>
    /// <returns>True if the order was acknowledged, false otherwise</returns>
    public Task<OpenOrderEventArgs> PlaceOrderAsync(Contract contract, Order order)
    {
        var taskSource = new TaskCompletionSource<OpenOrderEventArgs>();

        if(order.OrderId ==0)
            order.OrderId = GetNextValidOrderId();

        EventHandler<OpenOrderEventArgs> openOrderEventCallback  = null;
        EventHandler<ErrorEventArgs>     orderErrorEventCallback = null;

        void clearHandler()
        {
            this.TwsCallbackHandler_.OpenOrderEvent -= openOrderEventCallback;
            this.TwsCallbackHandler_.ErrorEvent     -= orderErrorEventCallback;
        }

        openOrderEventCallback = (sender, eventArgs) =>
        {
            if (eventArgs.OrderId == order.OrderId)
            {
                if (eventArgs.OrderState.Status == TwsOrderStatus.Submitted ||
                    eventArgs.OrderState.Status == TwsOrderStatus.Presubmitted ||
                    eventArgs.OrderState.Status == TwsOrderStatus.Filled)
                {

                    Logger?.Info($"IbClient PlaceOrderAsync({order.OrderId}, {contract}, {order}) Finished:{eventArgs}");

                    // Unregister the callbacks
                    clearHandler();
                    taskSource.TrySetResult(eventArgs);
                }
                else
                    Logger?.Warn($"Abnormal PlaceOrderAsync({eventArgs}):{eventArgs.OrderState}");
            }
        };

        orderErrorEventCallback = (sender, eventArgs) =>
        {
            if (order.OrderId == eventArgs.Id)
            {   
                // ib的错误和正常的状态混在一起，这里只处理明确的错误，实际很多应该作为wanrning处理，现在这样，导致不能处理所以情况
                // TwsErrorCodes.OrderMessageError // 如果 lot被修改，也会发送这个错误，但其实订单时正确完成了，这个时候不能作为error处理
                //if (// 以下是明确错误的情况
                //    eventArgs.ErrorCode == TwsErrorCodes.InvalidOrderType ||
                //    eventArgs.ErrorCode == TwsErrorCodes.AmbiguousContract ||
                //    eventArgs.ErrorCode == TwsErrorCodes.OrderRejected
                //    || eventArgs.ErrorCode ==TwsErrorCodes.InvalidEndTime //[errorMsg]:End Time: The date, time, or time-zone entered is invalid. The correct format is yyyymmdd hh:mm:ss xx/xxxx where yyyymmdd and xx/xxxx are optional
                //    || eventArgs.ErrorCode == TwsErrorCodes.OrderNotSupportFractionalQuantity 
                //    )
                if (eventArgs.ErrorCode != TwsErrorCodes.OrderMessageError)
                {
                    // Unregister the callbacks
                    clearHandler();
                    taskSource.TrySetException(new TwsException(eventArgs));
                }
                //Logger?.Error($"Ib PlaceOrderAsync error:{eventArgs.ErrorCode} {eventArgs.ErrorMessage}");

                Logger?.Error($"IbClient PlaceOrderAsync({order.OrderId}, {contract}, {order}) Error:[{eventArgs.ErrorCode}]{eventArgs.ErrorMessage}");
            }
        };

        this.TwsCallbackHandler_.ErrorEvent     += orderErrorEventCallback;
        this.TwsCallbackHandler_.OpenOrderEvent += openOrderEventCallback;


        //CancellationTokenSource cancellationToken = new CancellationTokenSource(TimeoutMilliseconds);
        CancellationTokenSource cancellationToken = new CancellationTokenSource(15000);  // 大部分情况下，15s能得到return
        cancellationToken.Token.Register(() =>
        {
            if (!taskSource.Task.IsCompleted)
            {
                Logger?.Error($"IbClient PlaceOrderAsync({order.OrderId}, {contract}, {order}) Timedout.");
                clearHandler();
                taskSource.TrySetCanceled();
            }
        });

        this.ClientSocket_.placeOrder(order.OrderId, contract, order);
        Logger?.Info($"IbClient PlaceOrderAsync({order.OrderId}, {contract}, {order})...");

        return taskSource.Task;
    }

    public List<Order> MakeBracketOrders(Contract contract,             EOrderActions action,                    EOrderTypeTws orderType,              double  quantity,
                                         double?  price         = null, ETifTws       tif         = ETifTws.GTC, double?       takeProfitPrice = null, double? stopLossPrice = null,
                                         ETifTws? takeProfitTif = null, ETifTws?      stopLossTif = null)
    {
        var parentOrderId = GetNextValidOrderId();

        List<Order> lo = new();
        //This will be our main or "parent" order
        Order parent = new Order();
        parent.OrderId   = parentOrderId;
        parent.Action    = action.ToString();
        parent.OrderType = orderType.GetDescription();
        //parent.OrderType     = "LMT";
        parent.TotalQuantity = Convert.ToDecimal(quantity);
        if (orderType == EOrderTypeTws.Stop)
            parent.AuxPrice = price.Value;
        else if (price != null)
            parent.LmtPrice = price.Value;
        parent.Tif = tif.ToString();
        //The parent and children orders will need this attribute set to false to prevent accidental executions.
        //The LAST CHILD will have it set to true, 
        parent.Transmit = takeProfitPrice == null && stopLossPrice == null ? true : false;
        lo.Add(parent);

        if (takeProfitPrice != null)
        {
            Order takeProfit = new Order();
            takeProfit.OrderId = GetNextValidOrderId();
            //takeProfit.Action        = action.Equals("BUY") ? "SELL" : "BUY";
            takeProfit.Action        = action.Reverse().ToString();
            takeProfit.OrderType     = "LMT";
            takeProfit.TotalQuantity = Convert.ToDecimal(quantity);
            takeProfit.LmtPrice      = takeProfitPrice.Value;
            takeProfit.ParentId      = parentOrderId;
            takeProfit.Tif           = takeProfitTif?.ToString() ?? tif.ToString();
            takeProfit.Transmit      = stopLossPrice == null ? true : false;
            lo.Add(takeProfit);
        }

        if (stopLossPrice != null)
        {
            Order stopLoss = new Order();
            stopLoss.OrderId = GetNextValidOrderId();
            //stopLoss.Action    = action.Equals("BUY") ? "SELL" : "BUY";
            stopLoss.Action    = action.Reverse().ToString();
            stopLoss.OrderType = "STP";
            //Stop trigger price
            stopLoss.AuxPrice      = stopLossPrice.Value;
            stopLoss.TotalQuantity = Convert.ToDecimal(quantity);
            stopLoss.ParentId      = parentOrderId;
            //In this case, the low side order will be the last child being sent. Therefore, it needs to set this attribute to true 
            //to activate all its predecessors
            stopLoss.Transmit = true;
            stopLoss.Tif      = stopLossTif?.ToString() ?? tif.ToString();
            lo.Add(stopLoss);
        }

        return lo;
    }



    // https://interactivebrokers.github.io/tws-api/bracket_order.html
    public Task<List<OpenOrderEventArgs>> PlaceBracketOrderAsync(Contract contract, EOrderActions action, EOrderTypeTws orderType, double quantity, double? limitPrice=null,  ETifTws tif = ETifTws.GTC, double? takeProfitPrice = null, double? stopLossPrice = null,
        ETifTws? takeProfitTif = null, ETifTws? stopLossTif = null)
    {
        var lo = MakeBracketOrders(contract, action, orderType, quantity, limitPrice, tif, takeProfitPrice, stopLossPrice, takeProfitTif, stopLossTif);
        return PlaceBracketOrderAsync(contract, lo);
    }

    public Task<List<OpenOrderEventArgs>> PlaceBracketOrderAsync(Contract contract, List<Order> lo)
    {
        int parentOrderId = lo[0].OrderId;
      
        var                      taskSource = new TaskCompletionSource<List<OpenOrderEventArgs>>();
        //List<OpenOrderEventArgs> ret        = new();

        Dictionary<int, OpenOrderEventArgs> ret        = new();

        EventHandler<OpenOrderEventArgs> openOrderEventCallback  = null;
        EventHandler<ErrorEventArgs>     orderErrorEventCallback = null;

        void clearHandler()
        {
            this.TwsCallbackHandler_.OpenOrderEvent -= openOrderEventCallback;
            this.TwsCallbackHandler_.ErrorEvent     -= orderErrorEventCallback;
        }

        openOrderEventCallback = (sender, eventArgs) =>
        {
            // 开3个单子，会获得6次回调
            /*
                orderStatus: orderId:313 status:PreSubmitted filled:0 remainning200
                Error: [id]:314 [errorCode]:399 [errorMsg]:Order Message: 买 200 EUR.USD Forex Warning: Your order size is below the EUR 20000 IdealPro minimum and will be routed as an odd lot order. [advancedOrderRejectJson]:
                orderStatus: orderId:314 status:PreSubmitted filled:0 remainning200
                orderStatus: orderId:314 status:PreSubmitted filled:0 remainning200
                Error: [id]:315 [errorCode]:399 [errorMsg]:Order Message: 买 200 EUR.USD Forex Warning: Your order size is below the EUR 20000 IdealPro minimum and will be routed as an odd lot order. [advancedOrderRejectJson]:
                orderStatus: orderId:315 status:PreSubmitted filled:0 remainning200
                orderStatus: orderId:315 status:PreSubmitted filled:0 remainning200
                orderStatus: orderId:313 status:Submitted filled:0 remainning200

                // 每个单子有2个冗余，删掉一个
            */
            if (lo.Any(p=>p.OrderId == eventArgs.OrderId))
            {
                if (eventArgs.OrderState.Status == TwsOrderStatus.Submitted ||
                    eventArgs.OrderState.Status == TwsOrderStatus.Presubmitted ||
                    eventArgs.OrderState.Status == TwsOrderStatus.Filled)
                {

                    // 后面的覆盖前面的
                    ret[eventArgs.OrderId] = eventArgs;
                    if (ret.Count==lo.Count )
                    //&& eventArgs.OrderId == parentOrderId && (eventArgs.OrderState.Status == TwsOrderStatus.Presubmitted ||eventArgs.OrderState.Status == TwsOrderStatus.Submitted || eventArgs.OrderState.Status == TwsOrderStatus.Filled))
                    {
                        //Unregister the callbacks
                        clearHandler();
                        taskSource.TrySetResult(ret.Values.OrderBy(p=>p.OrderId).ToList());
                    }
                }
            }
        };

        orderErrorEventCallback = (sender, eventArgs) =>
        {
            if (lo.Any(p=>p.OrderId == eventArgs.Id))
            {   
                // ib的错误和正常的状态混在一起，这里只处理明确的错误，实际很多应该作为wanrning处理，现在这样，导致不能处理所以情况
                // TwsErrorCodes.OrderMessageError // 如果 lot被修改，也会发送这个错误，但其实订单时正确完成了，这个时候不能作为error处理
                //if (// 以下是明确错误的情况
                //    eventArgs.ErrorCode == TwsErrorCodes.InvalidOrderType ||
                //    eventArgs.ErrorCode == TwsErrorCodes.AmbiguousContract ||
                //    eventArgs.ErrorCode == TwsErrorCodes.OrderRejected
                //    || eventArgs.ErrorCode ==TwsErrorCodes.InvalidEndTime //[errorMsg]:End Time: The date, time, or time-zone entered is invalid. The correct format is yyyymmdd hh:mm:ss xx/xxxx where yyyymmdd and xx/xxxx are optional
                //    || eventArgs.ErrorCode == TwsErrorCodes.OrderNotSupportFractionalQuantity 
                //    )
                if (eventArgs.ErrorCode != TwsErrorCodes.OrderMessageError)
                {
                    // Unregister the callbacks
                    clearHandler();
                    taskSource.TrySetException(new TwsException(eventArgs));
                }
                //Logger?.Error($"Ib PlaceOrderAsync error:{eventArgs.ErrorCode} {eventArgs.ErrorMessage}");
            }
        };

        this.TwsCallbackHandler_.ErrorEvent     += orderErrorEventCallback;
        this.TwsCallbackHandler_.OpenOrderEvent += openOrderEventCallback;

        //CancellationTokenSource cancellationToken = new CancellationTokenSource(TimeoutMilliseconds);
        CancellationTokenSource cancellationToken = new CancellationTokenSource(50000);
        cancellationToken.Token.Register(() =>
        {
            clearHandler();
            taskSource.TrySetCanceled();
        });

        // 同时发送几个，直到最后一个order，Transmit设为true后，才会有回调
        foreach (var o in lo)
            this.ClientSocket_.placeOrder(o.OrderId, contract, o);

        return taskSource.Task;

    }

    public event EventHandler<OrderStatusEventArgs> OrderStatusEvent; // 订单状态变化时间
    public event EventHandler<OpenOrderEventArgs>   OpenOrderEvent;   // 新订单事件

    /// 订单成交后，将触发ExecutionDetailsEvent&CommissionReportEvent 这2个事件
    public event EventHandler<ExecutionDetailsEventArgs> ExecutionDetailsEvent;

    public event EventHandler<CommissionReport> CommissionReportEvent;


    /// <summary>
    /// Get a list of all the executions from TWS
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to cancel the request</param>
    /// <returns>A list of execution details events from TWS.</returns>
    public Task<List<OpenOrderEventArgs>> ReqOpenOrdersAsync()
    {
        List<OpenOrderEventArgs>            openOrderEvents          = new List<OpenOrderEventArgs>();
        var                                 taskSource               = new TaskCompletionSource<List<OpenOrderEventArgs>>();
        EventHandler<OpenOrderEventArgs>    openOrderEventHandler    = null;
        EventHandler<OpenOrderEndEventArgs> openOrderEndEventHandler = null;

        openOrderEventHandler = (sender, args) => { openOrderEvents.Add(args); };

        openOrderEndEventHandler = (sender, args) =>
        {
            // Cleanup the event handlers when the positions end event is called.
            clearHandler();
            taskSource.TrySetResult(openOrderEvents);
        };

        void clearHandler()
        {
            this.TwsCallbackHandler_.OpenOrderEvent    -= openOrderEventHandler;
            this.TwsCallbackHandler_.OpenOrderEndEvent -= openOrderEndEventHandler;
        }

        this.TwsCallbackHandler_.OpenOrderEvent    += openOrderEventHandler;
        this.TwsCallbackHandler_.OpenOrderEndEvent += openOrderEndEventHandler;

        // Set the operation to cancel after 5 seconds
        CancellationTokenSource tokenSource = new CancellationTokenSource(TimeoutMilliseconds);
        tokenSource.Token.Register(() =>
        {
            clearHandler();
            taskSource.TrySetCanceled();
        });

        this.ClientSocket_.reqAllOpenOrders();

        return taskSource.Task;
    }


    public Task<List<CompletedOrderEventArgs>> ReqCompletedOrdersAsync()
    {
        List<CompletedOrderEventArgs>         completedOrders                = new List<CompletedOrderEventArgs>();
        var                                   taskSource                     = new TaskCompletionSource<List<CompletedOrderEventArgs>>();
        EventHandler<CompletedOrderEventArgs> completedOrderEventHandler     = null;
        EventHandler                          completedOrdersEndEventHandler = null;

        completedOrderEventHandler = (sender, args) => { completedOrders.Add(args); };

        completedOrdersEndEventHandler = (sender, args) =>
        {
            // Cleanup the event handlers when the positions end event is called.
            clearHandler();
            taskSource.TrySetResult(completedOrders);
        };

        void clearHandler()
        {
            this.TwsCallbackHandler_.CompletedOrderEvent     -= completedOrderEventHandler;
            this.TwsCallbackHandler_.CompletedOrdersEndEvent -= completedOrdersEndEventHandler;
        }

        this.TwsCallbackHandler_.CompletedOrderEvent     += completedOrderEventHandler;
        this.TwsCallbackHandler_.CompletedOrdersEndEvent += completedOrdersEndEventHandler;

        // Set the operation to cancel after 5 seconds
        CancellationTokenSource tokenSource = new CancellationTokenSource(TimeoutMilliseconds);
        tokenSource.Token.Register(() =>
        {
            clearHandler();
            taskSource.TrySetCanceled();
        });

        this.ClientSocket_.reqCompletedOrders(false);

        return taskSource.Task;
    }


    public Task<List<ExecutionDetailsEventArgs>> ReqExecutionsAsync(ExecutionFilter filter)
    {
        int requestId = this.TwsRequestIdGenerator_.GetNextRequestId();
        ReqContracts[requestId] = new(null, null);


        List<ExecutionDetailsEventArgs>         executions                = new List<ExecutionDetailsEventArgs>();
        var                                   taskSource                     = new TaskCompletionSource<List<ExecutionDetailsEventArgs>>();
        EventHandler<ExecutionDetailsEventArgs> executionDetailsEventHandler     = null;
        EventHandler<ExecutionDetailsEndEventArgs> executionDetailsEndEventHandler = null;

        executionDetailsEventHandler = (sender, args) =>
        {
            if(args.RequestId == requestId)
                executions.Add(args);
        };

        executionDetailsEndEventHandler = (sender, args) =>
        {
            if (args.RequestId == requestId)
            {
                // Cleanup the event handlers when the positions end event is called.
                clearHandler();
                taskSource.TrySetResult(executions);
            }
        };

        void clearHandler()
        {
            this.TwsCallbackHandler_.ExecutionDetailsEvent     -= executionDetailsEventHandler;
            this.TwsCallbackHandler_.ExecutionDetailsEndEvent -= executionDetailsEndEventHandler;
        }

        this.TwsCallbackHandler_.ExecutionDetailsEvent     += executionDetailsEventHandler;
        this.TwsCallbackHandler_.ExecutionDetailsEndEvent += executionDetailsEndEventHandler;

        // Set the operation to cancel after 5 seconds
        CancellationTokenSource tokenSource = new CancellationTokenSource(TimeoutMilliseconds);
        tokenSource.Token.Register(() =>
        {
            clearHandler();
            taskSource.TrySetCanceled();
        });

        this.ClientSocket_.reqExecutions(requestId, filter);

        return taskSource.Task;
    }

    /// <summary>
    /// Cancels an order in TWS.
    /// </summary>
    /// <param name="orderId">The order Id to cancel</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation. True if the broker acknowledged the cancelation request, false otherwise.</returns>
    public Task<bool> CancelOrderAsync(int orderId)
    {
        var taskSource = new TaskCompletionSource<bool>();

        EventHandler<OrderStatusEventArgs> orderStatusEventCallback = null;
        EventHandler<ErrorEventArgs>       errorEventCallback       = null;

        orderStatusEventCallback = (sender, eventArgs) =>
        {
            if (eventArgs.OrderId == orderId)
            {
                Debug.WriteLine($"Cancel order status:{eventArgs.Status}");
                // 这里行为不统一，有的时候再发送submitted后，会再发送一个Cancelled状态，
                // 但大多数情况下，不会再更新状态
                if (eventArgs.Status == "Cancelled")
                    //if (eventArgs.Status == "Submitted")
                {
                    // Unregister the callbacks
                    clearHandler();

                    // Set the result
                    taskSource.TrySetResult(true);
                }
            }
        };

        errorEventCallback = (sender, eventArgs) =>
        {
            if (eventArgs.ErrorCode == TwsErrorCodes.OrderCancelled)
            {
                clearHandler();
                taskSource.TrySetResult(true);
            }

            if (eventArgs.ErrorCode == TwsErrorCodes.OrderCannotBeCancelled ||
                eventArgs.ErrorCode == TwsErrorCodes.OrderCannotBeCancelled2)
            {
                clearHandler();
                taskSource.TrySetException(new TwsException($"Order {eventArgs.Id} cannot be canceled", eventArgs.ErrorCode, eventArgs.Id));
            }
        };

        void clearHandler()
        {
            this.TwsCallbackHandler_.ErrorEvent       -= errorEventCallback;
            this.TwsCallbackHandler_.OrderStatusEvent -= orderStatusEventCallback;
        }

        // Set the operation to cancel after 5 seconds
        CancellationTokenSource tokenSource = new CancellationTokenSource(TimeoutMilliseconds);
        tokenSource.Token.Register(() =>
        {
            clearHandler();
            taskSource.TrySetCanceled();
        });

        this.TwsCallbackHandler_.OrderStatusEvent += orderStatusEventCallback;
        this.TwsCallbackHandler_.ErrorEvent       += errorEventCallback;

        this.ClientSocket_.cancelOrder(orderId, null);
        return taskSource.Task;
    }

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
    public Task SubTickByTickDataAsync(Contract contract, ETickByTickDataType tickType)
    {
        if (TickByTickSubscriptions.Any(p => p.Item1.EqualSimple(contract) && p.Item2 == tickType))
            return Task.CompletedTask;


        int requestId = this.TwsRequestIdGenerator_.GetNextRequestId();
        ReqContracts[requestId] = new(contract, tickType);

        var taskSource = new TaskCompletionSource();

        EventHandler<TwsEventArs<Contract, HistoricalTick>> tickByTickMidPointEventHandler = null;
        EventHandler<TwsEventArs<Contract, HistoricalTickLast>> tickByTickLastEventHandler = null;
        EventHandler<TwsEventArs<Contract, HistoricalTickLast>> tickByTickAllLastEventHandler = null;
        EventHandler<TwsEventArs<Contract, HistoricalTickBidAsk>> tickByTickBidAskEventHandler = null;

        EventHandler<HistoricalTicksEventArgs>       historicalTicksEventHandler       = null;
        EventHandler<HistoricalTicksBidAskEventArgs> historicalTicksBidAskEventHandler = null;
        EventHandler<HistoricalTicksLastEventArgs>   historicalTicksLastEventHandler   = null;


        EventHandler<ErrorEventArgs> errorEventHandler = null;

        void clearHandler(ETickByTickDataType type)
        {
            switch (type)
            {
                case ETickByTickDataType.MidPoint:
                    this.TwsCallbackHandler_.TickByTickMidPointEvent -= tickByTickMidPointEventHandler;
                    this.TwsCallbackHandler_.HistoricalTicksEvent -= historicalTicksEventHandler;
                    break;
                case
                    ETickByTickDataType.Last:
                    this.TwsCallbackHandler_.TickByTickLastEvent -= tickByTickLastEventHandler;
                    this.TwsCallbackHandler_.HistoricalTicksLastEvent -= historicalTicksLastEventHandler;
                    break;
                case
                    ETickByTickDataType.AllLast:
                    this.TwsCallbackHandler_.TickByTickAllLastEvent -= tickByTickAllLastEventHandler;
                    this.TwsCallbackHandler_.HistoricalTicksLastEvent -= historicalTicksLastEventHandler;
                    break;
                case
                    ETickByTickDataType.BidAsk:
                    this.TwsCallbackHandler_.TickByTickBidAskEvent -= tickByTickBidAskEventHandler;
                    this.TwsCallbackHandler_.HistoricalTicksBidAskEvent -= historicalTicksBidAskEventHandler;
                    break;
            }

            this.TwsCallbackHandler_.ErrorEvent -= errorEventHandler;
        }

        void sucess(ETickByTickDataType type)
        {
            clearHandler(type);
            var tu = new Tuple<Contract, ETickByTickDataType>(contract, type);
            TickByTickSubscriptions_.Add(tu);
            TickByTickSubscriptionReqs_[requestId] = tu;

            taskSource.TrySetResult();
        }

        tickByTickMidPointEventHandler = (sender, args) =>
        {
            if (args.RequestId == requestId) { sucess(ETickByTickDataType.MidPoint); }
        };
        tickByTickLastEventHandler = (sender, args) =>
        {
            if (args.RequestId == requestId) { sucess(ETickByTickDataType.Last); }
        };
        tickByTickAllLastEventHandler = (sender, args) =>
        {
            if (args.RequestId == requestId) { sucess(ETickByTickDataType.AllLast); }
        };
        tickByTickBidAskEventHandler = (sender, args) =>
        {
            if (args.RequestId == requestId) { sucess(ETickByTickDataType.BidAsk); }
        };

        historicalTicksEventHandler = (sender, args) =>
        {
            if (args.RequestId == requestId) { sucess(ETickByTickDataType.MidPoint); }
        };

        historicalTicksBidAskEventHandler = (sender, args) =>
        {
            if (args.RequestId == requestId) { sucess(ETickByTickDataType.BidAsk); }
        };

        historicalTicksLastEventHandler = (sender, args) =>
        {
            if (args.RequestId == requestId) { sucess(tickType); }
        };



        errorEventHandler = (sender, args) =>
        {
            if (args.Id == requestId)
            {
                //todo:warn
                //this.CancelHistoricalData(requestId);

                clearHandler(tickType);
                taskSource.TrySetException(new TwsException(args));
            }
        };


        switch (tickType)
        {
            case ETickByTickDataType.MidPoint:
                this.TwsCallbackHandler_.TickByTickMidPointEvent += tickByTickMidPointEventHandler;
                this.TwsCallbackHandler_.HistoricalTicksEvent += historicalTicksEventHandler;
                break;
            case
                ETickByTickDataType.Last:
                this.TwsCallbackHandler_.TickByTickLastEvent += tickByTickLastEventHandler;
                this.TwsCallbackHandler_.HistoricalTicksLastEvent += historicalTicksLastEventHandler;
                break;
            case
                ETickByTickDataType.AllLast:
                this.TwsCallbackHandler_.TickByTickAllLastEvent += tickByTickAllLastEventHandler;
                this.TwsCallbackHandler_.HistoricalTicksLastEvent += historicalTicksLastEventHandler;
                break;
            case
                ETickByTickDataType.BidAsk:
                this.TwsCallbackHandler_.TickByTickBidAskEvent += tickByTickBidAskEventHandler;
                this.TwsCallbackHandler_.HistoricalTicksBidAskEvent += historicalTicksBidAskEventHandler;
                break;
        }
        this.TwsCallbackHandler_.ErrorEvent += errorEventHandler;


        // Set the operation to cancel after 20 s
        CancellationTokenSource tokenSource = new CancellationTokenSource(20 * 1000);
        tokenSource.Token.Register(() =>
        {
            //todo:warn
            //this.CancelHistoricalData(requestId);

            clearHandler(tickType);

            taskSource.TrySetCanceled();
        });

        // https://interactivebrokers.github.io/tws-api/tick_data.html
        /*
         The tick type field is case sensitive - it must be BidAsk, Last, AllLast, MidPoint. AllLast has additional trade types such as combos, derivatives, and average price trades which are not included in Last.
        Tick-by-tick data for options is currently only available historically and not in real time.
        Tick-by-tick data for indices is only provided for indices which are on CME.
        Tick-by-tick data is not available for combos.
        No more than 1 tick-by-tick request can be made for the same instrument within 15 seconds.
        */
        // 注意： * @param numberOfTicks - number of ticks.\n
        // 这个接口，当请求的tickbytick数据有的时候，会发送实时数据，如果不处于交易时段，则没有任何数据
        // 为了区分订阅数据是否成功，必须将@param numberOfTicks设为非0，如此,就算不处于交易时段，没有实时数据，也会发送一个历史数据过来
        //this.ClientSocket_.reqTickByTickData(requestId, contract, tickType.ToString(), 0, false);  // 这种方式，如果非交易时段，不会发送实时数据，无法区分是否成功
        // 另外： UnsubTickByTickData并不能保证取消订阅成功，因为ib的接口设计，无法知道是否取消成功，所以，同时要处理实时数据的影响
        this.ClientSocket_.reqTickByTickData(requestId, contract, tickType.ToString(), 1, false);

        return taskSource.Task;
    }

    public void UnsubTickByTickData(Contract contract, ETickByTickDataType tickType)
    {
        // contract必须一致，否则会出错
        var sub = TickByTickSubscriptions.FirstOrDefault(p => p.Item1.EqualSimple(contract) && p.Item2 == tickType);
        if (sub == null)
            return;

        var pair = TickByTickSubscriptionReqs_.FirstOrDefault(p => p.Value == sub);

        this.ClientSocket_.cancelTickByTickData(pair.Key);

        TickByTickSubscriptions_.Remove(sub);
        TickByTickSubscriptionReqs_.Remove(pair.Key);
    }

    public ReadOnlyObservableCollection<Tuple<Contract, ETickByTickDataType>> TickByTickSubscriptions { get => new(TickByTickSubscriptions_); }

    public event EventHandler<TwsEventArs<Contract, HistoricalTick>>       TickByTickMidPointEvent;
    public event EventHandler<TwsEventArs<Contract, HistoricalTickLast>>   TickByTickLastEvent;
    public event EventHandler<TwsEventArs<Contract, HistoricalTickLast>>   TickByTickAllLastEvent;
    public event EventHandler<TwsEventArs<Contract, HistoricalTickBidAsk>> TickByTickBidAskEvent;


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


    public Task SubRealtimeBarsAsync(Contract contract, EDataType datType, bool useRTH = true)
    {
        if (RealtimeBarsSubscriptions.Any(p => p == contract))
            return Task.CompletedTask;

        int requestId = this.TwsRequestIdGenerator_.GetNextRequestId();
        ReqContracts[requestId] = new(contract, null);

        var taskSource = new TaskCompletionSource();

        EventHandler<TwsEventArs<Contract, Bar>> realtimeBarEventHandler = null;
        EventHandler<ErrorEventArgs>             errorEventHandler       = null;

        realtimeBarEventHandler = (sender, args) =>
        {
            if (args.RequestId == requestId)
            {
                this.TwsCallbackHandler_.RealtimeBarEvent -= realtimeBarEventHandler;
                this.TwsCallbackHandler_.ErrorEvent       -= errorEventHandler;

                RealtimeBarsSubscriptions_.Add(contract);
                RealtimeBarsSubscriptionReqs_[requestId] = contract;

                taskSource.TrySetResult();
            }
        };

        errorEventHandler = (sender, args) =>
        {
            if (args.Id == requestId)
            {
                //todo:?
                //this.CancelRealtimeBars(requestId);

                // The error is associated with this request
                this.TwsCallbackHandler_.RealtimeBarEvent -= realtimeBarEventHandler;
                this.TwsCallbackHandler_.ErrorEvent       -= errorEventHandler;
                taskSource.TrySetException(new TwsException(args));
            }
        };

        this.TwsCallbackHandler_.RealtimeBarEvent += realtimeBarEventHandler;
        this.TwsCallbackHandler_.ErrorEvent       += errorEventHandler;


        // Set the operation to cancel after 1 minute
        CancellationTokenSource tokenSource = new CancellationTokenSource(TimeoutMilliseconds);
        tokenSource.Token.Register(() =>
        {
            this.TwsCallbackHandler_.RealtimeBarEvent -= realtimeBarEventHandler;
            this.TwsCallbackHandler_.ErrorEvent       -= errorEventHandler;
            taskSource.TrySetCanceled();
        });


        this.ClientSocket_.reqRealTimeBars(
                                           requestId,
                                           contract,
                                           5, // only function on 5
                                           datType.ToString(),
                                           useRTH,
                                           null);

        return taskSource.Task;
    }

    public void UnsubRealtimeBars(Contract contract)
    {
        var sub = RealtimeBarsSubscriptions.FirstOrDefault(p => p.EqualSimple(contract));
        if (sub == null)
            return;

        var pair = RealtimeBarsSubscriptionReqs_.FirstOrDefault(p => p.Value == sub);

        this.ClientSocket_.cancelRealTimeBars(pair.Key);

        RealtimeBarsSubscriptions_.Remove(sub);
        RealtimeBarsSubscriptionReqs_.Remove(pair.Key);
    }

    public ReadOnlyObservableCollection<Contract> RealtimeBarsSubscriptions { get => new(RealtimeBarsSubscriptions_); }

    public event EventHandler<TwsEventArs<Contract, Bar>> RealtimeBarEvent;

#endregion

#region Positions

    /// <summary>
    /// Get a list of all the positions in TWS.
    /// </summary>
    /// <returns>A list of position status events from TWS.</returns>
    public Task<List<PositionStatusEventArgs>> ReqPositions()
    {
        List<PositionStatusEventArgs>              positionStatusEvents            = new List<PositionStatusEventArgs>();
        var                                        taskSource                      = new TaskCompletionSource<List<PositionStatusEventArgs>>();
        EventHandler<PositionStatusEventArgs>      positionStatusEventHandler      = null;
        EventHandler<RequestPositionsEndEventArgs> requestPositionsEndEventHandler = null;

        positionStatusEventHandler = (sender, args) => { positionStatusEvents.Add(args); };

        requestPositionsEndEventHandler = (sender, args) =>
        {
            clearHandler();

            taskSource.TrySetResult(positionStatusEvents);
        };

        this.TwsCallbackHandler_.PositionStatusEvent      += positionStatusEventHandler;
        this.TwsCallbackHandler_.RequestPositionsEndEvent += requestPositionsEndEventHandler;

        void clearHandler()
        {
            // Cleanup the event handlers when the positions end event is called.
            this.TwsCallbackHandler_.PositionStatusEvent      -= positionStatusEventHandler;
            this.TwsCallbackHandler_.RequestPositionsEndEvent -= requestPositionsEndEventHandler;
        }

        // Set the operation to cancel after 1 minute
        CancellationTokenSource tokenSource = new CancellationTokenSource(60 * 1000);
        tokenSource.Token.Register(() =>
        {
            clearHandler();

            taskSource.TrySetCanceled();
        });


        this.ClientSocket_.reqPositions();

        return taskSource.Task;
    }


    public event EventHandler<PositionStatusEventArgs> PositionStatusEvent;

    /// <summary>
    /// Sends a message to TWS telling it to stop sending position information through the socket.
    /// </summary>
    public void UnsubPositions() { this.ClientSocket_.cancelPositions(); }

#endregion


    public Dictionary<int, Tuple<Contract, object?>> ReqContracts = new();


    /// <summary>
    /// Gets the next request id
    /// </summary>
    /// <returns>The next request id</returns>
    public int GetNextRequestId() { return this.TwsRequestIdGenerator_.GetNextRequestId(); }


    public int GetNextValidOrderId()
    {
        if (ConnectionState_ != EConnectionState.Connected)
            throw new InvalidOperationException("Not connected");

        var nextId = Interlocked.Read(ref NextValidOrderId_);
        var r      = Interlocked.Increment(ref NextValidOrderId_);
        return (int)nextId;
    }

    protected long NextValidOrderId_;


#region Fields

    private bool ThreadRunning_ = false;
    private object ReaderLock_  = new object();

//    private   bool            _Connected;
    protected EConnectionState ConnectionState_;

    private TwsCallbackHandler TwsCallbackHandler_ = null;

    //private TwsClientSocket    ClientSocket_       = null;
    private EClientSocket ClientSocket_ = null;

    private EReaderSignal Signal_;


    private TwsRequestIdGenerator TwsRequestIdGenerator_ = null;

    /// <summary>
    /// The background thread that will await the Signal_ and send events to the callback handler
    /// </summary>
    private Thread ReaderThread_;

    protected ObservableCollection<string> Accounts_ = new ObservableCollection<string>();


    protected ObservableCollection<Tuple<Contract, ETickByTickDataType>> TickByTickSubscriptions_    = new ObservableCollection<Tuple<Contract, ETickByTickDataType>>();
    protected Dictionary<int, Tuple<Contract, ETickByTickDataType>>      TickByTickSubscriptionReqs_ = new();

    protected ObservableCollection<Contract> RealtimeBarsSubscriptions_    = new();
    protected Dictionary<int, Contract>      RealtimeBarsSubscriptionReqs_ = new();

    protected object Lock_ = new object();

#endregion
}