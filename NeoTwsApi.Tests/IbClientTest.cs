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

        var ret = await client.ReqContractAsync(contract);
        // Assert
        ret.First().Should().NotBeNull();

        Debug.WriteLine(ret.Dump());
    }

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
        Contract    contract = XauusdContract_CMDTY;
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
            TotalQuantity = 2000
        };

        ExecutionDetailsEventArgs details = null;
        CommissionReport          cr      = null;

        EventHandler<ExecutionDetailsEventArgs> executionDetailsEventHandler = (s, e) => { details = e; };
        EventHandler<CommissionReport>          commissionReportHandler      = (s, e) => { cr      = e; };


        client.ExecutionDetailsEvent += executionDetailsEventHandler;
        client.CommissionReportEvent += commissionReportHandler;


        // Call the API
        var successfullyPlaced = await client.PlaceOrderAsync(contract, order);

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