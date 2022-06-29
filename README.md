# NeoTwsApi
Easy to use C# TWS api.

This project is inpired by AutoFinance.Broker, which is very good, but still have some bugs and flaws.
Thanks for your great efforts taken guys.

## 0. IIbClient -- IbApi Interface
```cs
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

...
}
```
This IIbClient interface is everything you needed to communicate with Tws.


## 1. Connction
### ConnectionState
```cs
public enum EConnectionState
{
    Disconnected =0,
    Connecting,
    Connected,
    Disconnecting,
}

 /// <summary>
 /// Gets a value indicating whether is the client connected to tws
 /// </summary>
 EConnectionState ConnectionState { get; }
```
### Connect
```cs
IIbClient client = new IbClient(TestConstants.Host, TestConstants.Port, TestConstants.ClientId, defaultLogger); // defaultLogger - can be null
bool connected = await client.ConnectAsync();
connected.Should().BeTrue();
```
### Disconnect
```cs
await client.DisconnectAsync();
client.ConnectionState.Should().Be(EConnectionState.Disconnected);
```

## 2. Account Management
### Accounts 
```cs
ReadOnlyObservableCollection<string> Accounts { get; }
```
### Get AccountDetails
```cs 
var acc = client.Accounts.FirstOrDefault();
var ret = await client.ReqAccountDetailsAsync(acc);
ret.Should().NotBeEmpty();
```

## 3. Contract Management
### Get ContractDetails
```cs
Contract EurContract = new Contract()
{
    Symbol   = "EUR",
    SecType  = ESecTypeTws.CASH.ToString(),
    Currency = ECurrencyTws.USD.ToString(),
    Exchange = EExchangeTws.IDEALPRO.ToString()
};
var ret = await client.ReqContractAsync(EurContract);
ret.First().Should().NotBeNull();
```

### Requests matching stock symbols
```cs
var ret = await client.ReqMatchingSymbolsAsync("MSFT");
ret.Should().NotBeEmpty();
```

## 4. Historical Data Management
### Get historical data
```cs
Contract    contract = XauusdContract_CMDTY;
DurationTws duration = new DurationTws(3, EDurationStep.D);
DateTime    end      = 17.March(2022).At(23, 59);

var ret = await client.ReqHistoricalDataAsync(contract, end, duration, ETimeFrameTws.H1, EDataType.MIDPOINT);
ret.Should().NotBeEmpty();
```

## 5. Streaming Data
Tws do provide a few data formats streamable, but only tick by tick data is useful really.
### Tick by tick data format
```cs
public enum ETickByTickDataType
{
    Last,
    AllLast,         
    BidAsk,
    MidPoint
}
```
### Current Subscriptions
```cs
ReadOnlyObservableCollection<Tuple<Contract, ETickByTickDataType>> TickByTickSubscriptions { get; }
```

### Tick by tick data comming handler
```cs
event EventHandler<TwsEventArs<Contract, HistoricalTick>>       TickByTickMidPointEvent;
event EventHandler<TwsEventArs<Contract, HistoricalTickLast>>   TickByTickLastEvent;
event EventHandler<TwsEventArs<Contract, HistoricalTickLast>>   TickByTickAllLastEvent;
event EventHandler<TwsEventArs<Contract, HistoricalTickBidAsk>> TickByTickBidAskEvent;
```

### Use case
```cs
Contract contract = new Contract();
contract.Symbol   = "EUR";
contract.SecType  = "CASH";
contract.Currency = "USD";
contract.Exchange = "IDEALPRO";

var historicalTickBidAsks = new List<HistoricalTickBidAsk>();
client.TickByTickBidAskEvent += (s, e) =>
{
    // store tickbytickdata
    historicalTickBidAsks.Add(e.Arg2);
    Debug.WriteLine(e.Arg2.Dump());
};

// subscribe a contract with particula tick by tick data format
await client.SubTickByTickDataAsync(contract, ETickByTickDataType.BidAsk);

client.TickByTickSubscriptions.Should().NotBeEmpty();
historicalTickBidAsks.Should().NotBeEmpty();

// cancel subsciption
client.UnsubTickByTickData(contract, ETickByTickDataType.BidAsk);
client.TickByTickSubscriptions.Should().BeEmpty();
```

## 6. Order Management
### Place an order
```cs
 // Initialize the contract
Contract contract = new Contract();
contract.Symbol   = "EUR";
contract.SecType  = "CASH";
contract.Currency = "USD";
contract.Exchange = "IDEALPRO";
// Initialize the order
Order order = new Order
{
    Action        = "BUY",
    OrderType     = "MKT",
    TotalQuantity = 2000
};
ExecutionDetailsEventArgs details = null;
CommissionReport          cr      = null;
EventHandler<ExecutionDetailsEventArgs> executionDetailsEventHandler = (s, e) => { details = e; };
EventHandler<CommissionReport>          commissionReportHandler      = (s, e) => { cr      = e; };
// observe on order's Execution Details
client.ExecutionDetailsEvent += executionDetailsEventHandler;
// observe on Commission Report
client.CommissionReportEvent += commissionReportHandler;

// Place the order
var successfullyPlaced = await client.PlaceOrderAsync(contract, order);
// Assert
successfullyPlaced.Should().NotBeNull();
// a few moments later
await Task.Delay(3000);
details.Should().NotBeNull();
cr.Should().NotBeNull();
client.ExecutionDetailsEvent -= executionDetailsEventHandler;
client.CommissionReportEvent -= commissionReportHandler;
```
### Requests open orders
```cs
var ret = await client.RequestOpenOrdersAsync();
ret.Should().NotBeEmpty();
```
### Cancels a pending order
```cs
var ret = await client.CancelOrderAsync(successfullyPlaced.OrderId);  // orderId from the PlaceOrderAsync() return
ret.Should().BeTrue();
```
## 7. Position Management
### Request all Positions
```cs
List<PositionStatusEventArgs> status = new();
EventHandler<PositionStatusEventArgs> positionStatusHandler = (s, e) => { status.Add(e); };
client.PositionStatusEvent += positionStatusHandler;

var ret = await client.RequestPositions(); // request Position will also subscribe to position change event
ret.Should().NotBeEmpty();
```

### Unsubscribe position change event
```cs
client.UnsubPositions();
```





