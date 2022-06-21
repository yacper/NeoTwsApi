/********************************************************************
    created:	2022/6/21 1:41:06
    author:		rush
    email:		yacper@gmail.com	
	
    purpose:
    modifiers:	用户服务
*********************************************************************/

using AutoFinance.Broker.InteractiveBrokers.Controllers;
using AutoFinance.Broker.InteractiveBrokers.Wrappers;
using IBApi;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using NLog;

namespace NeoTwsApi;

public class IbClient : ObservableObject, IIbClient
{
    public string Host { get; protected set; }

    public int Port { get; protected set; }

    public int ClientId { get; protected set; }

    public int TimeoutMillseconds { get; protected set; } = 5000;

    public ILogger? Logger { get; protected set; }


    public bool Connected { get => _Connected; protected set => SetProperty(ref _Connected, value); }

    public int ServerVersion { get; protected set; } = 0;

    public string ServerTime { get; protected set; } = null;


    public IbClient(string host, int port, int clientId, ILogger? logger = null)
    {
        Host     = host;
        Port     = port;
        ClientId = clientId;
        Logger   = logger;

        clientSocket = new TwsClientSocket(new EClientSocket(twsCallbackHandler, signal));
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
        var taskSource = new TaskCompletionSource<bool>();

        void ConnectionAcknowledgementCallback(object? sender, EventArgs eventArgs)
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

            Connected     = true;
            ServerVersion = clientSocket.EClientSocket.ServerVersion;
            ServerTime    = clientSocket.EClientSocket.ServerTime;
            taskSource.TrySetResult(true);
        }

        // Set the operation to cancel after 5 seconds
        CancellationTokenSource tokenSource = new CancellationTokenSource(this.TimeoutMillseconds);
        tokenSource.Token.Register(() =>
        {
            taskSource.TrySetCanceled();
        });

        this.twsCallbackHandler.ConnectionAcknowledgementEvent += ConnectionAcknowledgementCallback;
        this.clientSocket.Connect(this.Host, this.Port, this.ClientId);
        return taskSource.Task;
    }

    public Task DisconnectAsync()
    {
        var taskSource = new TaskCompletionSource<bool>();
        this.twsCallbackHandler.ConnectionClosedEvent += (sender, eventArgs) =>
        {
            // todo:Abort the reader thread


            Connected = false;
            taskSource.TrySetResult(true);
        };

        // Set the operation to cancel after 5 seconds
        CancellationTokenSource tokenSource = new CancellationTokenSource(TimeoutMillseconds);
        tokenSource.Token.Register(() => { taskSource.TrySetCanceled(); });

        this.clientSocket.Disconnect();

        return taskSource.Task;
    }


    private bool _Connected;

    private TwsCallbackHandler twsCallbackHandler = new TwsCallbackHandler();

    private TwsClientSocket clientSocket = null;

    private EReaderSignal signal = new EReaderMonitorSignal();

    private TwsRequestIdGenerator twsRequestIdGenerator = new TwsRequestIdGenerator();

    /// <summary>
    /// The background thread that will await the signal and send events to the callback handler
    /// </summary>
    private Thread readerThread;
}