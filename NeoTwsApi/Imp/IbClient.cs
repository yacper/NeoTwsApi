/********************************************************************
    created:	2022/6/21 1:41:06
    author:		rush
    email:		yacper@gmail.com	
	
    purpose:
    modifiers:	用户服务
*********************************************************************/

using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using AutoFinance.Broker.InteractiveBrokers;
using AutoFinance.Broker.InteractiveBrokers.Constants;
using AutoFinance.Broker.InteractiveBrokers.Wrappers;
using IBApi;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using NLog;
using AutoFinance.Broker.InteractiveBrokers.EventArgs;
using Microsoft.VisualBasic.CompilerServices;
using MoreLinq;
using NeoTwsApi.Enums;
using NeoTwsApi.EventArgs;
using NeoTwsApi.Exceptions;
using NeoTwsApi.Helpers;
using NeoTwsApi.Imp;
using ErrorEventArgs = NeoTwsApi.EventArgs.ErrorEventArgs;

namespace NeoTwsApi;

public class IbClient : ObservableObject, IIbClient
{
    public string Host { get; protected set; }

    public int Port { get; protected set; }

    public int ClientId { get; protected set; }

    public int TimeoutMilliseconds { get; protected set; } = 5000;

    public ILogger? Logger { get; protected set; }


    public bool Connected { get => _Connected; protected set => SetProperty(ref _Connected, value); }

    public int ServerVersion { get; protected set; } = 0;

    public string ServerTime { get; protected set; } = null;


    public ReadOnlyObservableCollection<string> Accounts { get => new(_Accounts); }


    public IbClient(string host, int port, int clientId, ILogger? logger = null)
    {
        Host     = host;
        Port     = port;
        ClientId = clientId;
        Logger   = logger;

        twsCallbackHandler                         =  new TwsCallbackHandler(this);
        clientSocket                               =  new EClientSocket(twsCallbackHandler, signal);
        twsCallbackHandler.TickByTickBidAskEvent   += TwsCallbackHandler_TickByTickBidAskEvent;
        twsCallbackHandler.TickByTickMidPointEvent += TwsCallbackHandler_TickByTickMidPointEvent;
        twsCallbackHandler.TickByTickLastEvent     += TwsCallbackHandler_TickByTickLastEvent;
        twsCallbackHandler.TickByTickAllLastEvent  += TwsCallbackHandlerTickByTickAllLastEvent;

        twsCallbackHandler.RealtimeBarEvent += TwsCallbackHandler_RealtimeBarEvent;
        //clientSocket   = new TwsClientSocket(_EClientSocket);
        //clientSocket   = _EClientSocket;

        twsCallbackHandler.UpdateAccountValueEvent += _OnUpdateAccountValueEvent;
    }

    /// <summary>
    /// Gets the next request id
    /// </summary>
    /// <returns>The next request id</returns>
    public int GetNextRequestId() { return this.twsRequestIdGenerator.GetNextRequestId(); }


    public int GetNextValidOrderId()
    {
        var nextId = Interlocked.Read(ref NextValidOrderId_);
        var r = Interlocked.Increment(ref NextValidOrderId_);
        return (int)nextId;
    }

    protected long NextValidOrderId_;





    private void TwsCallbackHandlerTickByTickAllLastEvent(object? sender, TwsEventArs<Contract, HistoricalTickLast> e) { TickByTickAllLastEvent?.Invoke(this, e); }

    private void TwsCallbackHandler_TickByTickLastEvent(object? sender, TwsEventArs<Contract, HistoricalTickLast> e) { TickByTickLastEvent?.Invoke(this, e); }

    private void TwsCallbackHandler_TickByTickMidPointEvent(object? sender, TwsEventArs<Contract, HistoricalTick> e) { TickByTickMidPointEvent?.Invoke(this, e); }

    private void TwsCallbackHandler_TickByTickBidAskEvent(object? sender, TwsEventArs<Contract, HistoricalTickBidAsk> e) { TickByTickBidAskEvent?.Invoke(this, e); }

    private void TwsCallbackHandler_RealtimeBarEvent(object? sender, TwsEventArs<Contract, Bar> e) { RealtimeBarEvent?.Invoke(this, e); }

    public async Task<bool> ConnectedAsync()
    {
        if (!this.Connected)
        {
            await this._ConnectAsync();

            // Sometimes TWS flushes the socket on a new connection
            // And the socket will get really fucked up any commands come in during that time
            // Just wait 5 seconds for it to finish
            await Task.Delay(5000);
        }

        return Connected;
    }

    /// <summary>
    /// Connect to the TWS socket and launch a background thread to begin firing the events.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    private Task _ConnectAsync()
    {
        Logger?.Info($"Start ConnectAsync:{this.Dump()}");

        var                                taskSource              = new TaskCompletionSource<bool>();
        EventHandler<NextValidIdEventArgs> nextValidIdEventHandler = null;

        void ConnectionAcknowledgementCallback(object? sender, System.EventArgs eventArgs)
        {
            // When the connection is acknowledged, create a reader to consume messages from the TWS.
            // The EReader will consume the incoming messages and the callback handler will begin to fire events.
            this.twsCallbackHandler.ConnectionAcknowledgementEvent -= ConnectionAcknowledgementCallback;
            var reader = new EReader(this.clientSocket, this.signal);
            reader.Start();

            this.readerThread = new Thread(
                                           () =>
                                           {
                                               while (true)
                                               {
                                                   this.signal.waitForSignal();
                                                   reader.processMsgs();
                                               }
                                           })
                { IsBackground = true };
            this.readerThread.Start();
        }

        nextValidIdEventHandler = (s, e) =>
        {
            this.twsCallbackHandler.NextValidIdEvent -= nextValidIdEventHandler;

            //nextValidId() indicate really connected
            OnConnected(e.OrderId);

            taskSource.TrySetResult(true);
        };

        // Set the operation to cancel after 5 seconds
        CancellationTokenSource tokenSource = new CancellationTokenSource(this.TimeoutMilliseconds);
        tokenSource.Token.Register(() => { taskSource.TrySetCanceled(); });

        this.twsCallbackHandler.ConnectionAcknowledgementEvent += ConnectionAcknowledgementCallback;
        this.twsCallbackHandler.NextValidIdEvent               += nextValidIdEventHandler;

        this.clientSocket.eConnect(this.Host, this.Port, this.ClientId);
        return taskSource.Task;
    }

    public Task DisconnectAsync()
    {
        if (!Connected)
            return Task.CompletedTask;

        var taskSource = new TaskCompletionSource<bool>();
        this.twsCallbackHandler.ConnectionClosedEvent += (sender, eventArgs) =>
        {
            // todo:Abort the reader thread


            OnDisconnected();
            taskSource.TrySetResult(true);
        };

        // Set the operation to cancel after 5 seconds
        CancellationTokenSource tokenSource = new CancellationTokenSource(TimeoutMilliseconds);
        tokenSource.Token.Register(() => { taskSource.TrySetCanceled(); });

        this.clientSocket.eDisconnect();

        return taskSource.Task;
    }

    protected void OnConnected(int nextValidId = 0)
    { 
        Interlocked.Exchange(ref NextValidOrderId_, nextValidId);

        twsRequestIdGenerator = new TwsRequestIdGenerator();

        Connected     = true;
        ServerVersion = clientSocket.ServerVersion;
        ServerTime    = clientSocket.ServerTime;


        //twsCallbackHandler.Accounts.ForEach(p => _Accounts.Add(p));


        Logger?.Info($"Connected:{this.Dump()}");
    }

    public void OnAccountsReccieved(string accountsList) // 当连接建立后，tws后会主动发送managedAccounts
    {
        accountsList.Split(',').ForEach(p => _Accounts.Add(p));
    }

    protected void OnDisconnected()
    {
        Logger?.Info($"DisConnected:{this.Dump()}");
        Connected = false;
        _Accounts.Clear();
    }


#region Account

    public Task<ConcurrentDictionary<string, string>> ReqAccountDetailsAsync(string accountId)
    {
        var taskSource = new TaskCompletionSource<ConcurrentDictionary<string, string>>();

        EventHandler<AccountDownloadEndEventArgs> accountDownloadEndHandler = (s, e) => { taskSource.TrySetResult(this._AccountUpdates); };

        this.twsCallbackHandler.AccountDownloadEndEvent -= accountDownloadEndHandler; // always clear
        this.twsCallbackHandler.AccountDownloadEndEvent += accountDownloadEndHandler;

        CancellationTokenSource tokenSource = new CancellationTokenSource(TimeoutMilliseconds);
        tokenSource.Token.Register(() => { taskSource.TrySetCanceled(); });

        this.clientSocket.reqAccountUpdates(true, accountId);

        return taskSource.Task;
    }


    /// <summary>
    /// Run when an account value is updated
    /// </summary>
    /// <param name="sender">The sender</param>
    /// <param name="eventArgs">The event arguments</param>
    private void _OnUpdateAccountValueEvent(object sender, UpdateAccountValueEventArgs eventArgs)
    {
        this._AccountUpdates.AddOrUpdate(eventArgs.Key, eventArgs.Value, (key, value) =>
        {
            return eventArgs.Value; // Always take the most recent result
        });
    }

    /// <summary>
    /// The account updates dictionary
    /// </summary>
    private ConcurrentDictionary<string, string> _AccountUpdates = new();

#endregion


#region contract

    /// <summary>
    /// Gets a contract by request.
    /// </summary>
    /// <param name="contract">The requested contract.</param>
    /// <returns>The details of the contract</returns>
    public Task<List<ContractDetails>> ReqContractAsync(Contract contract)
    {
        int                   requestId           = this.twsRequestIdGenerator.GetNextRequestId();
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
                this.twsCallbackHandler.ContractDetailsEvent -=
                    contractDetailsEventHandler;
                this.twsCallbackHandler.ContractDetailsEndEvent -=
                    contractDetailsEndEventHandler;
                this.twsCallbackHandler.ErrorEvent -= errorEventHandler;

                taskSource.TrySetResult(contractDetailsList);
            }
        };

        errorEventHandler = (sender, args) =>
        {
            if (args.Id == requestId)
            {
                // The error is associated with this request
                this.twsCallbackHandler.ContractDetailsEvent    -= contractDetailsEventHandler;
                this.twsCallbackHandler.ContractDetailsEndEvent -= contractDetailsEndEventHandler;
                this.twsCallbackHandler.ErrorEvent              -= errorEventHandler;

                taskSource.TrySetException(new TwsException(args));
            }
        };

        // Set the operation to cancel after 5 seconds
        CancellationTokenSource tokenSource = new CancellationTokenSource(TimeoutMilliseconds);
        tokenSource.Token.Register(() => { taskSource.TrySetCanceled(); });

        this.twsCallbackHandler.ContractDetailsEvent    += contractDetailsEventHandler;
        this.twsCallbackHandler.ContractDetailsEndEvent += contractDetailsEndEventHandler;
        this.twsCallbackHandler.ErrorEvent              += errorEventHandler;

        this.clientSocket.reqContractDetails(requestId, contract);
        return taskSource.Task;
    }


    public Task<List<ContractDescription>> ReqMatchingSymbolsAsync(string pattern)
    {
        int                       requestId           = this.twsRequestIdGenerator.GetNextRequestId();
        List<ContractDescription> contractDetailsList = new List<ContractDescription>();

        var taskSource = new TaskCompletionSource<List<ContractDescription>>();

        EventHandler<TwsEventArs<ContractDescription[]>> symbolSamplesHandler = null;

        symbolSamplesHandler = (sender, args) =>
        {
            if (args.RequestId == requestId)
            {
                contractDetailsList.AddRange(args.Arg);

                twsCallbackHandler.SymbolSamplesEvent -= symbolSamplesHandler;
                taskSource.TrySetResult(contractDetailsList);
            }
        };
        //EventHandler<ErrorEventArgs> errorEventHandler = (sender, args) =>
        //{
        //    if (args.Id == requestId)
        //    {
        //        // The error is associated with this request
        //        this.twsCallbackHandler.ContractDetailsEvent -= contractDetailsEventHandler;
        //        this.twsCallbackHandler.ContractDetailsEndEvent -= contractDetailsEndEventHandler;
        //        this.twsCallbackHandler.ErrorEvent -= errorEventHandler;

        //        taskSource.TrySetException(new TwsException(args));
        //    }
        //};

        // Set the operation to cancel after 5 seconds
        CancellationTokenSource tokenSource = new CancellationTokenSource(TimeoutMilliseconds);
        tokenSource.Token.Register(() => { taskSource.TrySetCanceled(); });

        this.twsCallbackHandler.SymbolSamplesEvent += symbolSamplesHandler;
        //this.twsCallbackHandler.ErrorEvent += errorEventHandler;

        clientSocket.reqMatchingSymbols(requestId, pattern);
        return taskSource.Task;
    }

#endregion

#region HistoricalData

    public Task<List<Bar>> ReqHistoricalDataAsync(Contract contract, DateTime begin, DateTime end, ETimeFrameTws tf, EDataType dataType, bool useRth = true)
    {
        // covert [begin. end] to duration
        throw new NotImplementedException();
    }

    public Task<List<Bar>> ReqHistoricalDataAsync(Contract contract, DateTime end, DurationTws duration, ETimeFrameTws tf, EDataType dataType, bool useRth = true)
    {
        int            requestId    = this.twsRequestIdGenerator.GetNextRequestId();
        List<TagValue> chartOptions = null;

        string value = string.Empty;

        var taskSource = new TaskCompletionSource<List<Bar>>();

        EventHandler<TwsEventArs<Bar>>                historicalDataEventHandler    = null;
        EventHandler<TwsEventArs<DateTime, DateTime>> historicalDataEndEventHandler = null;
        EventHandler<ErrorEventArgs>                  errorEventHandler             = null;

        List<Bar> historicalDataList = new List<Bar>();

        historicalDataEventHandler = (sender, args) =>
        {
            if (args.RequestId == requestId) { historicalDataList.Add(args.Arg); }
        };

        historicalDataEndEventHandler = (sender, args) =>
        {
            if (args.RequestId == requestId)
            {
                this.twsCallbackHandler.HistoricalDataEvent    -= historicalDataEventHandler;
                this.twsCallbackHandler.HistoricalDataEndEvent -= historicalDataEndEventHandler;
                this.twsCallbackHandler.ErrorEvent             -= errorEventHandler;
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
                this.twsCallbackHandler.HistoricalDataEvent    -= historicalDataEventHandler;
                this.twsCallbackHandler.HistoricalDataEndEvent -= historicalDataEndEventHandler;
                this.twsCallbackHandler.ErrorEvent             -= errorEventHandler;
                taskSource.TrySetException(new TwsException(args));
            }
        };

        // Set the operation to cancel after 1 minute
        CancellationTokenSource tokenSource = new CancellationTokenSource(60 * 1000);
        tokenSource.Token.Register(() =>
        {
            //todo:warn
            //this.CancelHistoricalData(requestId);

            this.twsCallbackHandler.HistoricalDataEvent    -= historicalDataEventHandler;
            this.twsCallbackHandler.HistoricalDataEndEvent -= historicalDataEndEventHandler;
            this.twsCallbackHandler.ErrorEvent             -= errorEventHandler;

            taskSource.TrySetCanceled();
        });

        this.twsCallbackHandler.HistoricalDataEvent    += historicalDataEventHandler;
        this.twsCallbackHandler.HistoricalDataEndEvent += historicalDataEndEventHandler;
        this.twsCallbackHandler.ErrorEvent             += errorEventHandler;


        this.clientSocket.reqHistoricalData(
                                            requestId,
                                            contract,
                                            end.ToTwsDateTimeString(),
                                            duration.ToString(),
                                            tf.ToTwsString(),
                                            dataType.ToString(),
                                            useRth ? 1 : 0,
                                            1,     // default format date
                                            false, // not keep update
                                            chartOptions);

        return taskSource.Task;
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

        var orderId    = GetNextValidOrderId();

        EventHandler<OpenOrderEventArgs> openOrderEventCallback = null;
        EventHandler<ErrorEventArgs> orderErrorEventCallback = null;

        openOrderEventCallback = (sender, eventArgs) =>
        {
            if (eventArgs.OrderId == orderId)
            {
                if (eventArgs.OrderState.Status == TwsOrderStatus.Submitted ||
                    eventArgs.OrderState.Status == TwsOrderStatus.Presubmitted)
                {
                    // Unregister the callbacks
                    this.twsCallbackHandler.OpenOrderEvent -= openOrderEventCallback;
                    this.twsCallbackHandler.ErrorEvent     -= orderErrorEventCallback;

                    taskSource.TrySetResult(eventArgs);
                }
            }
        };

        orderErrorEventCallback = (sender, eventArgs) =>
        {
            if (orderId == eventArgs.Id)
            {
                if (
                    eventArgs.ErrorCode == TwsErrorCodes.InvalidOrderType ||
                    eventArgs.ErrorCode == TwsErrorCodes.AmbiguousContract ||
                    eventArgs.ErrorCode == TwsErrorCodes.OrderRejected)
                {
                    // Unregister the callbacks
                    this.twsCallbackHandler.OpenOrderEvent -= openOrderEventCallback;
                    this.twsCallbackHandler.ErrorEvent     -= orderErrorEventCallback;
                    taskSource.TrySetException(new TwsException(eventArgs));
                }
            }
        };

        this.twsCallbackHandler.ErrorEvent     += orderErrorEventCallback;
        this.twsCallbackHandler.OpenOrderEvent += openOrderEventCallback;


        CancellationTokenSource cancellationToken = new CancellationTokenSource(TimeoutMilliseconds);
        cancellationToken.Token.Register(() => { taskSource.TrySetCanceled(); });

        this.clientSocket.placeOrder(orderId, contract, order);
        return taskSource.Task;
    }

    /// <summary>
    /// Get a list of all the executions from TWS
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to cancel the request</param>
    /// <returns>A list of execution details events from TWS.</returns>
    public Task<List<OpenOrderEventArgs>> RequestOpenOrdersAsync()
    {
        List<OpenOrderEventArgs>            openOrderEvents          = new List<OpenOrderEventArgs>();
        var                                 taskSource               = new TaskCompletionSource<List<OpenOrderEventArgs>>();
        EventHandler<OpenOrderEventArgs>    openOrderEventHandler    = null;
        EventHandler<OpenOrderEndEventArgs> openOrderEndEventHandler = null;

        openOrderEventHandler = (sender, args) => { openOrderEvents.Add(args); };

        openOrderEndEventHandler = (sender, args) =>
        {
            // Cleanup the event handlers when the positions end event is called.
            this.twsCallbackHandler.OpenOrderEvent    -= openOrderEventHandler;
            this.twsCallbackHandler.OpenOrderEndEvent -= openOrderEndEventHandler;

            taskSource.TrySetResult(openOrderEvents);
        };

        this.twsCallbackHandler.OpenOrderEvent    += openOrderEventHandler;
        this.twsCallbackHandler.OpenOrderEndEvent += openOrderEndEventHandler;

        // Set the operation to cancel after 5 seconds
        CancellationTokenSource tokenSource = new CancellationTokenSource(TimeoutMilliseconds);
        tokenSource.Token.Register(() => { taskSource.TrySetCanceled(); });

        this.clientSocket.reqAllOpenOrders();

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
                if (eventArgs.Status == "Cancelled")
                {
                    // Unregister the callbacks
                    this.twsCallbackHandler.OrderStatusEvent -= orderStatusEventCallback;
                    this.twsCallbackHandler.ErrorEvent       -= errorEventCallback;

                    // Set the result
                    taskSource.TrySetResult(true);
                }
            }
        };

        errorEventCallback = (sender, eventArgs) =>
        {
            if (eventArgs.ErrorCode == TwsErrorCodes.OrderCancelled)
            {
                this.twsCallbackHandler.ErrorEvent -= errorEventCallback;
                this.twsCallbackHandler.OrderStatusEvent -= orderStatusEventCallback;
                taskSource.TrySetResult(true);
            }

            if (eventArgs.ErrorCode == TwsErrorCodes.OrderCannotBeCancelled ||
                eventArgs.ErrorCode == TwsErrorCodes.OrderCannotBeCancelled2)
            {
                this.twsCallbackHandler.ErrorEvent -= errorEventCallback;
                this.twsCallbackHandler.OrderStatusEvent -= orderStatusEventCallback;
                taskSource.TrySetException(new TwsException($"Order {eventArgs.Id} cannot be canceled", eventArgs.ErrorCode, eventArgs.Id));
            }
        };

        // Set the operation to cancel after 5 seconds
        CancellationTokenSource tokenSource = new CancellationTokenSource(TimeoutMilliseconds);
        tokenSource.Token.Register(() => { taskSource.TrySetCanceled(); });

        this.twsCallbackHandler.OrderStatusEvent += orderStatusEventCallback;
        this.twsCallbackHandler.ErrorEvent       += errorEventCallback;

        this.clientSocket.cancelOrder(orderId, null);
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
        if (TickByTickSubscriptions.Any(p => p.Item1 == contract && p.Item2 == tickType))
            return Task.CompletedTask;


        int requestId = this.twsRequestIdGenerator.GetNextRequestId();
        ReqContracts[requestId] = new(contract, tickType);

        var taskSource = new TaskCompletionSource();

        EventHandler<TwsEventArs<Contract, HistoricalTick>>       tickByTickMidPointEventHandler = null;
        EventHandler<TwsEventArs<Contract, HistoricalTickLast>>   tickByTickLastEventHandler     = null;
        EventHandler<TwsEventArs<Contract, HistoricalTickLast>>   tickByTickAllLastEventHandler  = null;
        EventHandler<TwsEventArs<Contract, HistoricalTickBidAsk>> tickByTickBidAskEventHandler   = null;

        EventHandler<ErrorEventArgs> errorEventHandler = null;

        void clearHandler(ETickByTickDataType type)
        {
            switch (type)
            {
                case ETickByTickDataType.MidPoint:
                    this.twsCallbackHandler.TickByTickMidPointEvent -= tickByTickMidPointEventHandler;
                    break;
                case
                    ETickByTickDataType.Last:
                    this.twsCallbackHandler.TickByTickLastEvent -= tickByTickLastEventHandler;
                    break;
                case
                    ETickByTickDataType.AllLast:
                    this.twsCallbackHandler.TickByTickAllLastEvent -= tickByTickAllLastEventHandler;
                    break;
                case
                    ETickByTickDataType.BidAsk:
                    this.twsCallbackHandler.TickByTickBidAskEvent -= tickByTickBidAskEventHandler;
                    break;
            }

            this.twsCallbackHandler.ErrorEvent -= errorEventHandler;
        }

        void sucess(ETickByTickDataType type)
        {
            clearHandler(type);
            var tu = new Tuple<Contract, ETickByTickDataType>(contract, type);
            _TickByTickSubscriptions.Add(tu);
            _TickByTickSubscriptionReqs[requestId] = tu;

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
                this.twsCallbackHandler.TickByTickMidPointEvent += tickByTickMidPointEventHandler;
                break;
            case ETickByTickDataType.Last:
                this.twsCallbackHandler.TickByTickLastEvent += tickByTickLastEventHandler;
                break;
            case ETickByTickDataType.AllLast:
                this.twsCallbackHandler.TickByTickAllLastEvent += tickByTickAllLastEventHandler;
                break;
            case ETickByTickDataType.BidAsk:
                this.twsCallbackHandler.TickByTickBidAskEvent += tickByTickBidAskEventHandler;
                break;
        }

        this.twsCallbackHandler.ErrorEvent += errorEventHandler;


        // Set the operation to cancel after 1 minute
        CancellationTokenSource tokenSource = new CancellationTokenSource(60 * 1000);
        tokenSource.Token.Register(() =>
        {
            //todo:warn
            //this.CancelHistoricalData(requestId);

            clearHandler(tickType);

            taskSource.TrySetCanceled();
        });

        this.clientSocket.reqTickByTickData(requestId, contract, tickType.ToString(), 0, false);

        return taskSource.Task;
    }

    public void UnsubTickByTickData(Contract contract, ETickByTickDataType tickType)
    {
        var sub = TickByTickSubscriptions.FirstOrDefault(p => p.Item1 == contract && p.Item2 == tickType);
        if (sub == null)
            return;

        var pair = _TickByTickSubscriptionReqs.FirstOrDefault(p => p.Value == sub);

        this.clientSocket.cancelTickByTickData(pair.Key);

        _TickByTickSubscriptions.Remove(sub);
        _TickByTickSubscriptionReqs.Remove(pair.Key);
    }

    public ReadOnlyObservableCollection<Tuple<Contract, ETickByTickDataType>> TickByTickSubscriptions { get => new(_TickByTickSubscriptions); }

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

        int requestId = this.twsRequestIdGenerator.GetNextRequestId();
        ReqContracts[requestId] = new(contract, null);

        var taskSource = new TaskCompletionSource();

        EventHandler<TwsEventArs<Contract, Bar>> realtimeBarEventHandler = null;
        EventHandler<ErrorEventArgs>             errorEventHandler       = null;

        realtimeBarEventHandler = (sender, args) =>
        {
            if (args.RequestId == requestId)
            {
                this.twsCallbackHandler.RealtimeBarEvent -= realtimeBarEventHandler;
                this.twsCallbackHandler.ErrorEvent       -= errorEventHandler;

                _RealtimeBarsSubscriptions.Add(contract);
                _RealtimeBarsSubscriptionReqs[requestId] = contract;

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
                this.twsCallbackHandler.RealtimeBarEvent -= realtimeBarEventHandler;
                this.twsCallbackHandler.ErrorEvent       -= errorEventHandler;
                taskSource.TrySetException(new TwsException(args));
            }
        };

        this.twsCallbackHandler.RealtimeBarEvent += realtimeBarEventHandler;
        this.twsCallbackHandler.ErrorEvent       += errorEventHandler;

        this.clientSocket.reqRealTimeBars(
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
        var sub = RealtimeBarsSubscriptions.FirstOrDefault(p => p == contract);
        if (sub == null)
            return;

        var pair = _RealtimeBarsSubscriptionReqs.FirstOrDefault(p => p.Value == sub);

        this.clientSocket.cancelRealTimeBars(pair.Key);

        _RealtimeBarsSubscriptions.Remove(sub);
        _RealtimeBarsSubscriptionReqs.Remove(pair.Key);
    }

    public ReadOnlyObservableCollection<Contract> RealtimeBarsSubscriptions { get => new(_RealtimeBarsSubscriptions); }

    public event EventHandler<TwsEventArs<Contract, Bar>> RealtimeBarEvent;

#endregion


    public Dictionary<int, Tuple<Contract, object?>> ReqContracts = new();

#region Fields

    private bool _Connected;

    private TwsCallbackHandler twsCallbackHandler = null;

    //private TwsClientSocket    clientSocket       = null;
    private EClientSocket clientSocket   = null;
    private EClientSocket _EClientSocket = null;

    private EReaderSignal signal = new EReaderMonitorSignal();

    private TwsRequestIdGenerator twsRequestIdGenerator = null;

    /// <summary>
    /// The background thread that will await the signal and send events to the callback handler
    /// </summary>
    private Thread readerThread;

    protected ObservableCollection<string> _Accounts = new ObservableCollection<string>();


    protected ObservableCollection<Tuple<Contract, ETickByTickDataType>> _TickByTickSubscriptions    = new ObservableCollection<Tuple<Contract, ETickByTickDataType>>();
    protected Dictionary<int, Tuple<Contract, ETickByTickDataType>>      _TickByTickSubscriptionReqs = new();

    protected ObservableCollection<Contract> _RealtimeBarsSubscriptions    = new();
    protected Dictionary<int, Contract>      _RealtimeBarsSubscriptionReqs = new();

#endregion
}