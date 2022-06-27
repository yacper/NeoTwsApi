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
using NUnit.Framework;

namespace NeoTwsApi.Tests
{
public class Tests
{
    IbClient client = new IbClient(TestConstants.Host, TestConstants.Port, TestConstants.ClientId);

    [SetUp]
    public async Task Setup()
    {
        // Setup
        bool connected = await client.ConnectedAsync();
        connected.Should().BeTrue();

        //client.Accounts.Should().NotBeEmpty();

        Debug.WriteLine(client.Dump());
    }

    //[Test]
    //public async Task ConnectAsync()
    //{
    //    // Setup
    // IbClient client = new IbClient(TestConstants.Host, TestConstants.Port, TestConstants.ClientId);
    //    bool connected = await client.ConnectedAsync();

    //    Debug.WriteLine(client.Dump());


    //    //await client.DisconnectAsync();

    //    //Debug.WriteLine(client.Dump());
    //}

#region Account

    [Test]
    public async Task ReqAccountDetailsAsync_Test()
    {
        var ret = await client.ReqAccountDetailsAsync(client.Accounts.FirstOrDefault());
        Debug.WriteLine(ret.Dump());

        ret.Should().NotBeEmpty();
    }

#endregion

#region Contract

    [Test]
    public async Task GetContractAsync_Test()
    {
        Contract contract = new Contract();
        contract.Symbol   = "EUR";
        contract.SecType  = "CASH";
        contract.Currency = "USD";
        contract.Exchange = "IDEALPRO";


        var ret = await client.ReqContractAsync(contract);
        Debug.WriteLine(ret.Dump());

        // Assert
        ret.First().Should().NotBeNull();
    }

    [Test]
    public async Task ReqMatchingSymbols_Test()
    {
        var ret = await client.ReqMatchingSymbolsAsync("Ib");
        Debug.WriteLine(ret.Dump());

        // Assert
        ret.First().Should().NotBeNull();
    }

#endregion

#region HistoricalData

    [Test]
    public async Task ReqHistoricalDataAsync_Test1()
    {
        Contract contract = new Contract();
        contract.Symbol   = "EUR";
        contract.SecType  = "CASH";
        contract.Currency = "USD";
        contract.Exchange = "IDEALPRO";

        DurationTws duration = new DurationTws(3, EDurationStep.D);
        DateTime    end      = 17.March(2022).At(23, 59);

        var ret = await client.ReqHistoricalDataAsync(contract, end, duration,
                                                      ETimeFrameTws.H1, EDataType.MIDPOINT);
        Debug.WriteLine(ret.Dump());

        // Assert
        ret.First().Should().NotBeNull();
    }

#endregion


#region Streaming Data

    [Test]
    public async Task SubTickByTickData_Test0()
    {
        Contract contract = new Contract();
        contract.Symbol   = "EUR2"; // bad contract
        contract.SecType  = "CASH";
        contract.Currency = "USD";
        contract.Exchange = "IDEALPRO";

        client.TickByTickBidAskEvent += (s, e) => { Debug.WriteLine(e.Arg2.Dump()); };


        //try
        //{
        //    await client.SubTickByTickDataAsync(contract, ETickByTickDataType.BidAsk);
        //}
        //catch (Exception e)
        //{
        //    e.Dump();
        //}

        var a = async () => { await client.SubTickByTickDataAsync(contract, ETickByTickDataType.BidAsk); };
        await a.Should().ThrowAsync<TwsException>();
    }


    [Test]
    public async Task SubTickByTickData_BidAsk()
    {
        Contract contract = new Contract();
        contract.Symbol   = "EUR";
        contract.SecType  = "CASH";
        contract.Currency = "USD";
        contract.Exchange = "IDEALPRO";

        var historicalTickBidAsks = new List<HistoricalTickBidAsk>();
        client.TickByTickBidAskEvent += (s, e) =>
        {
            historicalTickBidAsks.Add(e.Arg2);
            Debug.WriteLine(e.Arg2.Dump());
        };

        await client.SubTickByTickDataAsync(contract, ETickByTickDataType.BidAsk);

        await client.SubTickByTickDataAsync(contract, ETickByTickDataType.BidAsk);

        client.TickByTickSubscriptions.Should().NotBeEmpty();
        historicalTickBidAsks.Should().NotBeEmpty();


        // cancel
        client.UnsubTickByTickData(contract, ETickByTickDataType.BidAsk);
        client.TickByTickSubscriptions.Should().BeEmpty();
        await Task.Delay(5000);
    }

    [Test]
    public async Task SubTickByTickData_Last()
    {
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
        Contract contract = new Contract();
        contract.Symbol   = "EUR";
        contract.SecType  = "CASH";
        contract.Currency = "USD";
        contract.Exchange = "IDEALPRO";

        var bars = new List<Bar>();
        client.RealtimeBarEvent += (s, e) =>
        {
            bars.Add(e.Arg2);
            Debug.WriteLine(e.Arg2.Dump());
        };

        await client.SubRealtimeBarsAsync(contract, EDataType.MIDPOINT);
        await client.SubRealtimeBarsAsync(contract, EDataType.MIDPOINT); // return immediately

        client.RealtimeBarsSubscriptions.Should().NotBeEmpty();
        bars.Should().NotBeEmpty();

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


        var ret = await client.RequestOpenOrdersAsync();

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

        var ret = await client.CancelOrderAsync(successfullyPlaced.OrderId);

        // Assert
        ret.Should().BeTrue();
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
        var ret = await client.RequestPositions();
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