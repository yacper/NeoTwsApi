/********************************************************************
    created:	2022/6/21 1:41:06
    author:		rush
    email:		yacper@gmail.com	
	
    purpose:
    modifiers:	用户服务
*********************************************************************/

using System.Collections.ObjectModel;
using AutoFinance.Broker.InteractiveBrokers.Controllers;
using AutoFinance.Broker.InteractiveBrokers.Wrappers;
using IBApi;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using NLog;
using AutoFinance.Broker.InteractiveBrokers.EventArgs;
using AutoFinance.Broker.InteractiveBrokers.Exceptions;
using Microsoft.VisualBasic.CompilerServices;
using ErrorEventArgs = AutoFinance.Broker.InteractiveBrokers.EventArgs.ErrorEventArgs;
using MoreLinq;
using NeoTwsApi.Enums;
using NeoTwsApi.EventArgs;
using NeoTwsApi.Helpers;

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


    public IReadOnlyList<string> Accounts { get => _Accounts; }


    public IbClient(string host, int port, int clientId, ILogger? logger = null)
    {
        Host     = host;
        Port     = port;
        ClientId = clientId;
        Logger   = logger;

        _EClientSocket = new EClientSocket(twsCallbackHandler, signal);
        clientSocket   = new TwsClientSocket(_EClientSocket);
    }


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

        var taskSource = new TaskCompletionSource<bool>();

        void ConnectionAcknowledgementCallback(object? sender, System.EventArgs eventArgs)
        {
            // When the connection is acknowledged, create a reader to consume messages from the TWS.
            // The EReader will consume the incoming messages and the callback handler will begin to fire events.
            this.twsCallbackHandler.ConnectionAcknowledgementEvent -= ConnectionAcknowledgementCallback;
            var reader = new EReader(this.clientSocket.EClientSocket, this.signal);
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

            //todo: need a better way
            OnConnected();

            taskSource.TrySetResult(true);
        }

        // Set the operation to cancel after 5 seconds
        CancellationTokenSource tokenSource = new CancellationTokenSource(this.TimeoutMilliseconds);
        tokenSource.Token.Register(() => { taskSource.TrySetCanceled(); });

        this.twsCallbackHandler.ConnectionAcknowledgementEvent += ConnectionAcknowledgementCallback;
        this.clientSocket.Connect(this.Host, this.Port, this.ClientId);
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

        this.clientSocket.Disconnect();

        return taskSource.Task;
    }

    protected void OnConnected()
    {
        Logger?.Info($"Connected:{this.Dump()}");
        Connected     = true;
        ServerVersion = clientSocket.EClientSocket.ServerVersion;
        ServerTime    = clientSocket.EClientSocket.ServerTime;

        twsCallbackHandler.Accounts.ForEach(p => _Accounts.Add(p));
    }

    protected void OnDisconnected()
    {
        Logger?.Info($"DisConnected:{this.Dump()}");
        Connected = false;
        _Accounts.Clear();
    }

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

        this.clientSocket.ReqContractDetails(requestId, contract);
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

        _EClientSocket.reqMatchingSymbols(requestId, pattern);
        return taskSource.Task;
    }

#endregion

#region HistoricalData

    public Task<List<Bar>> ReqHistoricalDataAsync(Contract      contract, DateTime  begin,    DateTime end,
                                                  ETimeFrameTws tf,       EDataType dataType, bool     useRth = true)
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


        this.clientSocket.ReqHistoricalData(
                                            requestId,
                                            contract,
                                            end.ToString("yyyyMMdd HH:mm:ss"),
                                            TwsDuration.ToTwsDuration(begin, end, tf),
                                            tf.ToTwsString(),
                                            dataType.ToString(),
                                            useRth ? 1 : 0,
                                            1, // default format date
                                            chartOptions);

        return taskSource.Task;
    }

#endregion


#region Fields

    private bool _Connected;

    private TwsCallbackHandler twsCallbackHandler = new TwsCallbackHandler();

    private TwsClientSocket clientSocket   = null;
    private EClientSocket   _EClientSocket = null;

    private EReaderSignal signal = new EReaderMonitorSignal();

    private TwsRequestIdGenerator twsRequestIdGenerator = new TwsRequestIdGenerator();

    /// <summary>
    /// The background thread that will await the signal and send events to the callback handler
    /// </summary>
    private Thread readerThread;

    protected ObservableCollection<string> _Accounts = new ObservableCollection<string>();

#endregion
}