using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using IBApi;
using NeoTwsApi.Enums;
using NeoTwsApi.EventArgs;
using NeoTwsApi.Exceptions;
using NeoTwsApi.Helpers;
using NeoTwsApi.Imp;
using NLog;
using NLog.Config;
using NUnit.Framework;

namespace NeoTwsApi.Tests
{
public partial class Tests
{
    private IIbClient client = null;


    [OneTimeSetUp]
    public async Task Setup()
    {
        ILogger defaultLogger = null;
        LogManager.Configuration = new XmlLoggingConfiguration("NLog.config");
        defaultLogger            = LogManager.GetCurrentClassLogger();

        /// connect
        client = new IbClient(TestConstants.Host, TestConstants.Port, TestConstants.ClientId, defaultLogger); // defaultLogger - can be null
        bool connected = await client.ConnectAsync();
        connected.Should().BeTrue();

        Debug.WriteLine(client.Dump(new DumpOptions(){ExcludeProperties = new List<string>(){"Logger"}}));
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        await client.DisconnectAsync();

        client.ConnectionState.Should().Be(EConnectionState.Disconnected);
        Debug.WriteLine(client.Dump(new DumpOptions(){ExcludeProperties = new List<string>(){"Logger"}}));
    }

    [Test]
    public async Task Reconnect_Test()
    {
        await client.DisconnectAsync();
        client.ConnectionState.Should().Be(EConnectionState.Disconnected);
        Debug.WriteLine(client.Dump(new DumpOptions(){ExcludeProperties = new List<string>(){"Logger"}}));

        /// reconnect
        bool connected = await client.ConnectAsync();
        connected.Should().BeTrue();
        Debug.WriteLine(client.Dump(new DumpOptions(){ExcludeProperties = new List<string>(){"Logger"}}));

        // wait some time for account info
        await Task.Delay(5000);
    }

#region Account

    [Test]
    public async Task ReqAccountDetailsAsync_Test()
    {
        var acc = client.Accounts.FirstOrDefault();
        acc.Should().NotBeNullOrEmpty();

        var ret = await client.ReqAccountDetailsAsync(acc);
        ret.Should().NotBeEmpty();

        Debug.WriteLine(ret.Dump());
    }

#endregion

#region Contract

    [Test]
    public async Task GetContractAsync_Test()
    {
        Contract contract = QQQContract_ETF;

        //var ret = await client.ReqContractAsync(contract);
        //var ret = await client.ReqContractAsync(MsftContract);
        var ret = await client.ReqContractAsync(AaplContract);
        //var ret = await client.ReqContractAsync(EurContract);


        // Assert
        ret.First().Should().NotBeNull();

        Debug.WriteLine(ret.Dump());
    }
        /* aapl contract
    {ContractDetails}
  Contract: {Contract}
    ConId: 265598
    Symbol: "AAPL"
    SecType: "STK"
    LastTradeDateOrContractMonth: null
    Strike: 0
    Right: null
    Multiplier: null
    Exchange: "SMART"
    Currency: "USD"
    LocalSymbol: "AAPL"
    PrimaryExch: "NASDAQ"
    TradingClass: "NMS"
    IncludeExpired: false
    SecIdType: null
    SecId: null
    ComboLegsDescription: null
    ComboLegs: null
    DeltaNeutralContract: null
  MarketName: "NMS"
  MinTick: 0.01
  PriceMagnifier: 1
  OrderTypes: "ACTIVETIM,AD,ADJUST,ALERT,ALGO,ALLOC,AON,AVGCOST,BASKET,BENCHPX,CASHQTY,COND,CONDORDER,DARKONLY,DARKPOLL,DAY,DEACT,DEACTDIS,DEACTEOD,DIS,DUR,GAT,GTC,GTD,GTT,HID,IBKRATS,ICE,IMB,IOC,LIT,LMT,LOC,MIDPX,MIT,MKT,MOC,MTL,NGCOMB,NODARK,NONALGO,OCA,OPG,OPGREROUT,PEGBENCH,PEGMID,POSTATS,POSTONLY,PREOPGRTH,PRICECHK,REL,REL2MID,RELPCTOFS,RPI,RTH,SCALE,SCALEODD,SCALERST,SIZECHK,SMARTSTG,SNAPMID,SNAPMKT,SNAPREL,STP,STPLMT,SWEEP,TRAIL,TRAILLIT,TRAILLMT,TRAILMIT,WHATIF"
  ValidExchanges: "SMART,AMEX,NYSE,CBOE,PHLX,ISE,CHX,ARCA,ISLAND,DRCTEDGE,BEX,BATS,EDGEA,JEFFALGO,BYX,IEX,EDGX,FOXRIVER,PEARL,NYSENAT,LTSE,MEMX,IBEOS,OVERNIGHT,PSX"
  UnderConId: 0
  LongName: "APPLE INC"
  ContractMonth: null
  Industry: "Technology"
  Category: "Computers"
  Subcategory: "Computers"
  TimeZoneId: "US/Eastern"
  TradingHours: "20231205:0400-20231205:2000;20231206:0400-20231206:2000;20231207:0400-20231207:2000;20231208:0400-20231208:2000"
  LiquidHours: "20231205:0930-20231205:1600;20231206:0930-20231206:1600;20231207:0930-20231207:1600;20231208:0930-20231208:1600"
  EvRule: null
  EvMultiplier: 0
  AggGroup: 1
  SecIdList: ...
    {TagValue}
      Tag: "ISIN"
      Value: "US0378331005"
  UnderSymbol: null
  UnderSecType: null
  MarketRuleIds: "26,26,26,26,26,26,26,26,26,26,26,26,26,26,26,26,26,26,26,26,26,26,26,26,26"
  RealExpirationDate: null
  LastTradeTime: null
  StockType: "COMMON"
  Cusip: null
  Ratings: null
  DescAppend: null
  BondType: null
  CouponType: null
  Callable: false
  Putable: false
  Coupon: 0
  Convertible: false
  Maturity: null
  IssueDate: null
  NextOptionDate: null
  NextOptionType: null
  NextOptionPartial: false
  Notes: null
  MinSize: 0.0001
  SizeIncrement: 0.0001
  SuggestedSizeIncrement: 100
         
        Eur contract
{ContractDetails}
  Contract: {Contract}
    ConId: 12087792
    Symbol: "EUR"
    SecType: "CASH"
    LastTradeDateOrContractMonth: null
    Strike: 0
    Right: null
    Multiplier: null
    Exchange: "IDEALPRO"
    Currency: "USD"
    LocalSymbol: "EUR.USD"
    PrimaryExch: null
    TradingClass: "EUR.USD"
    IncludeExpired: false
    SecIdType: null
    SecId: null
    ComboLegsDescription: null
    ComboLegs: null
    DeltaNeutralContract: null
  MarketName: "EUR.USD"
  MinTick: 5E-05
  PriceMagnifier: 1
  OrderTypes: "ACTIVETIM,AD,ADJUST,ALERT,ALGO,ALLOC,AVGCOST,BASKET,CASHQTY,COND,CONDORDER,DAY,DEACT,DEACTDIS,DEACTEOD,GAT,GTC,GTD,GTT,HID,IOC,LIT,LMT,MIT,MKT,NONALGO,OCA,REL,RELPCTOFS,SCALE,SCALERST,STP,STPLMT,TRAIL,TRAILLIT,TRAILLMT,TRAILMIT,WHATIF"
  ValidExchanges: "IDEALPRO"
  UnderConId: 0
  LongName: "European Monetary Union Euro"
  ContractMonth: null
  Industry: null
  Category: null
  Subcategory: null
  TimeZoneId: "US/Eastern"
  TradingHours: "20231204:1715-20231205:1700;20231205:1715-20231206:1700;20231206:1715-20231207:1700;20231207:1715-20231208:1700;20231209:CLOSED;20231210:1715-20231211:1700"
  LiquidHours: "20231204:1715-20231205:1700;20231205:1715-20231206:1700;20231206:1715-20231207:1700;20231207:1715-20231208:1700;20231209:CLOSED;20231210:1715-20231211:1700"
  EvRule: null
  EvMultiplier: 0
  AggGroup: 4
  SecIdList: null
  UnderSymbol: null
  UnderSecType: null
  MarketRuleIds: "3188"
  RealExpirationDate: null
  LastTradeTime: null
  StockType: null
  Cusip: null
  Ratings: null
  DescAppend: null
  BondType: null
  CouponType: null
  Callable: false
  Putable: false
  Coupon: 0
  Convertible: false
  Maturity: null
  IssueDate: null
  NextOptionDate: null
  NextOptionType: null
  NextOptionPartial: false
  Notes: null
  MinSize: 0.01
  SizeIncrement: 0.01
  SuggestedSizeIncrement: 0.01
         *
         */

    [Test]
    public async Task ReqMatchingSymbols_Test()
    {
        var ret = await client.ReqMatchingSymbolsAsync("MSFT");
        ret.Should().NotBeEmpty();

        Debug.WriteLine(ret.Dump());
    }

#endregion

#region HistoricalData

    [Test]
    public async Task ReqHistoricalDataAsync_Test()
    {
        //Contract    contract = XauusdContract_CMDTY;
        Contract    contract = AaplContract;
        DurationTws duration = new DurationTws(3, EDurationStep.D);
        DateTime    end      = 14.September(2022).At(0, 0);

        var ret = await client.ReqHistoricalDataAsync(contract, end, duration,
                                                      ETimeFrameTws.H1, EDataType.MIDPOINT);
        ret.Should().NotBeEmpty();

        Debug.WriteLine(ret.Dump());

    }
    [Test]
    public async Task ReqHistoricalDataAsync_Test2()
    {
        Contract    contract = XauusdContract_CMDTY;
        DurationTws duration = new DurationTws(1, EDurationStep.Y);
        DateTime    end      = 24.May(2022).At(16, 00);

        var ret = await client.ReqHistoricalDataAsync(contract, end, duration,
                                                      ETimeFrameTws.S5, EDataType.MIDPOINT);
        ret.Should().NotBeEmpty();

        Debug.WriteLine(ret.Dump());
    }

    [Test]
    public async Task ReqHistoricalDataAsync_Test3()
    {
        Contract contract = XauusdContract_CMDTY;
        DateTime start    = DateTime.Parse("2022/7/4");
        DateTime end    = DateTime.Parse("2022/7/5");
        DateTime end2    = DateTime.Parse("2022/7/6");

        //{
        //    var ret = await client.ReqHistoricalDataAsync(contract, start, end,
        //                                                  ETimeFrameTws.D1, EDataType.MIDPOINT);
        //    ret.Count.Should().Be(1);
        //}
        //{
        //    var ret = await client.ReqHistoricalDataAsync(contract, start, end,
        //                                                  ETimeFrameTws.M5, EDataType.MIDPOINT);
        //        // 周一从早上6:00开始的k线
        //    ret.Count.Should().Be(216);
        //    ret.FirstOrDefault().Time().Should().Be(DateTime.Parse("2022/7/4 06:00:00"));
        //    ret.LastOrDefault().Time().Should().Be(DateTime.Parse("2022/7/4 23:55:00"));
        //}
        {
            var ret = await client.ReqHistoricalDataAsync(contract, end, end2,
                                                          ETimeFrameTws.M5, EDataType.MIDPOINT);
                // 周二从早上00:00开始的k线
                // 2:10闭盘，6:00重新开盘
            ret.Count.Should().Be(243);
            ret.FirstOrDefault().Time().Should().Be(DateTime.Parse("2022/7/5 00:00:00"));
            ret.LastOrDefault().Time().Should().Be(DateTime.Parse("2022/7/5 23:55:00"));
        }




        //Debug.WriteLine(ret.Dump());
    }


#endregion


#region Streaming Data

    [Test]
    public async Task SubTickByTickData_ShouldThrow()
    {
        Contract contract = new Contract();
        contract.Symbol   = "EUR2"; // bad contract
        contract.SecType  = "CASH";
        contract.Currency = "USD";
        contract.Exchange = "IDEALPRO";

        client.TickByTickBidAskEvent += (s, e) => { Debug.WriteLine(e.Arg2.Dump()); };

        var a = async () => { await client.SubTickByTickDataAsync(contract, ETickByTickDataType.BidAsk); };
        await a.Should().ThrowAsync<TwsException>();
    }


    [Test]
    public async Task SubTickByTickData_BidAsk()
    {
        Contract            contract     = EurContract;  // 默认只有eur支持tickbytickdata， 并且只有midpoint和bidask
        //Contract            contract     = AaplContract;  // 默认只有eur支持tickbytickdata， 并且只有midpoint和bidask
        ETickByTickDataType tickDataType = ETickByTickDataType.BidAsk;

        var      historicalTickBidAsks = new List<HistoricalTickBidAsk>();
        client.TickByTickBidAskEvent += (s, e) =>
        {
            historicalTickBidAsks.Add(e.Arg2);
            Debug.WriteLine($"{contract.Symbol} BidAsk:");
            Debug.WriteLine(e.Arg2.Dump());
        };
        var      historicalTickMidPoints = new List<HistoricalTick>();
        client.TickByTickMidPointEvent += (s, e) =>
        {
            historicalTickMidPoints.Add(e.Arg2);
            Debug.WriteLine($"{contract.Symbol} MidPoint:");
            Debug.WriteLine(e.Arg2.Dump());
        };


        await client.SubTickByTickDataAsync(contract, tickDataType);

        await client.SubTickByTickDataAsync(contract, ETickByTickDataType.MidPoint);
        client.TickByTickSubscriptions.Should().NotBeEmpty();
        historicalTickBidAsks.Should().NotBeEmpty();

        await Task.Delay(3000);

        // cancel
        client.UnsubTickByTickData(contract, tickDataType);
        client.UnsubTickByTickData(contract, ETickByTickDataType.MidPoint);
        client.TickByTickSubscriptions.Should().BeEmpty();
        await Task.Delay(2000);
    }

    [Test]
    public async Task SubTickByTickData_MidPoint()
    {
        return;
        Contract contract = new Contract();
        contract.Symbol   = "EUR";
        contract.SecType  = "CASH";
        contract.Currency = "USD";
        contract.Exchange = "IDEALPRO";

        var historicalTickBidAsks = new List<HistoricalTickLast>();
        client.TickByTickLastEvent += (s, e) =>
        {
            historicalTickBidAsks.Add(e.Arg2);
            Debug.WriteLine(e.Arg2.Dump());
        };

        await client.SubTickByTickDataAsync(contract, ETickByTickDataType.Last);

        historicalTickBidAsks.Should().NotBeEmpty();
    }


    [Test]
    public async Task SubRealtimeBarsAsync_Test()
    {
        Contract contract = EurContract;
        var      bars     = new List<Bar>();
        client.RealtimeBarEvent += (s, e) =>
        {
            bars.Add(e.Arg2);
            Debug.WriteLine(e.Arg2.Dump());
        };

        await client.SubRealtimeBarsAsync(contract, EDataType.MIDPOINT);
        await client.SubRealtimeBarsAsync(contract, EDataType.MIDPOINT); // return immediately

        client.RealtimeBarsSubscriptions.Should().NotBeEmpty();
        bars.Should().NotBeEmpty();

        await Task.Delay(5000);
        // cancel
        client.UnsubRealtimeBars(contract);
        client.RealtimeBarsSubscriptions.Should().BeEmpty();
        await Task.Delay(5000);
    }

#endregion

#region Orders

    [Test]
    public async Task PlaceOrderAsync_Test()
    {
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
            TotalQuantity = 2000,
            //TotalQuantity = 2000.01m,   在tws客户端上，eur最小支持0.01单子，也可以被执行，但是通过api无法执行，返回TwsErrorCodes.OrderNotSupportFractionalQuantity
            Tif = ETifTws.GTC.ToString()
        };


        // Initialize the order
        Order closeOrder = new Order
        {
            Action        = "SELL",
            OrderType     = "MKT",
            TotalQuantity = 12000M,
            Tif           = ETifTws.GTC.ToString()
        };

        ExecutionDetailsEventArgs details = null;
        CommissionReport          cr      = null;

        EventHandler<ExecutionDetailsEventArgs> executionDetailsEventHandler = (s, e) => { details = e; };
        EventHandler<CommissionReport>          commissionReportHandler      = (s, e) => { cr      = e; };


        client.ExecutionDetailsEvent += executionDetailsEventHandler;
        client.CommissionReportEvent += commissionReportHandler;


        // Call the API
        var successfullyPlaced = await client.PlaceOrderAsync(contract, order);
        //var successfullyPlaced = await client.PlaceOrderAsync(contract, closeOrder);

        // Assert
        successfullyPlaced.Should().NotBeNull();
        Debug.WriteLine(successfullyPlaced.Dump());

        await Task.Delay(3000);

        details.Should().NotBeNull();
        Debug.WriteLine(details.Dump());

        cr.Should().NotBeNull();
        Debug.WriteLine(cr.Dump());

        client.ExecutionDetailsEvent -= executionDetailsEventHandler;
        client.CommissionReportEvent -= commissionReportHandler;
    }

    [Test]
    public async Task RequestOpenOrdersAsync_Test()
    {
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
            OrderType     = "LMT",
            TotalQuantity = 5,
            LmtPrice      = 1
        };

        var successfullyPlaced = await client.PlaceOrderAsync(contract, order);
        successfullyPlaced.Should().NotBeNull();


        var ret = await client.ReqOpenOrdersAsync();

        // Assert
        ret.Should().NotBeEmpty();


        // cancel
        var cancelRet = await client.CancelOrderAsync(successfullyPlaced.OrderId);
        // Assert
        cancelRet.Should().BeTrue();
    }

    [Test]
    public async Task CancelOrderAsync_Test()
    {
        //await Setup();

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
            OrderType     = "LMT",
            TotalQuantity = 20000,
            LmtPrice      = 1
        };

        var successfullyPlaced = await client.PlaceOrderAsync(contract, order);
        successfullyPlaced.Should().NotBeNull();

        var ret = await client.CancelOrderAsync(successfullyPlaced.OrderId);
        ret.Should().BeTrue();


        //await TearDown();
    }

#endregion

#region Positions

    [Test]
    public async Task PositionsManage_Test()
    {
        // Initialize the contract
        Contract contract = new Contract();
        contract.Symbol   = "EUR";
        contract.SecType  = "CASH";
        contract.Currency = "USD";
        contract.Exchange = "IDEALPRO";

        List<PositionStatusEventArgs> status = new();

        EventHandler<PositionStatusEventArgs> positionStatusHandler = (s, e) => { status.Add(e); };

        client.PositionStatusEvent += positionStatusHandler;

        {
            Order order = new Order
            {
                Action        = "BUY",
                OrderType     = "MKT",
                TotalQuantity = 20000
            };

            // Call the API
            var successfullyPlaced = await client.PlaceOrderAsync(contract, order);

            // Assert
            successfullyPlaced.Should().NotBeNull();
            Debug.WriteLine(successfullyPlaced.Dump());

            await Task.Delay(3000);
        }
        var ret = await client.ReqPositions();
        ret.Should().NotBeEmpty();

        {
            Order orderSell = new Order
            {
                Action        = "SELL",
                OrderType     = "MKT",
                TotalQuantity = 20000
            };

            var sellPlaced = await client.PlaceOrderAsync(contract, orderSell);
            sellPlaced.Should().NotBeNull();

            await Task.Delay(3000);
        }

        status.Should().NotBeEmpty();
        status.ForEach(p => Debug.WriteLine(p.Dump()));

        // unsub position change event
        client.UnsubPositions();
        status.Clear();

        {
            Order order = new Order
            {
                Action        = "BUY",
                OrderType     = "MKT",
                TotalQuantity = 20000
            };

            // Call the API
            var successfullyPlaced = await client.PlaceOrderAsync(contract, order);

            // Assert
            successfullyPlaced.Should().NotBeNull();
            Debug.WriteLine(successfullyPlaced.Dump());

            await Task.Delay(3000);
        }
        {
            Order orderSell = new Order
            {
                Action        = "SELL",
                OrderType     = "MKT",
                TotalQuantity = 20000
            };

            var sellPlaced = await client.PlaceOrderAsync(contract, orderSell);
            sellPlaced.Should().NotBeNull();

            await Task.Delay(3000);
        }

        status.Should().BeEmpty();

        client.PositionStatusEvent -= positionStatusHandler;
    }

#endregion
}
}