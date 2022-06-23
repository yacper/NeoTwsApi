using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AutoFinance.Broker.InteractiveBrokers.Exceptions;
using FluentAssertions;
using FluentAssertions.Extensions;
using IBApi;
using NeoTwsApi.Enums;
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

        var ret = await client.ReqHistoricalDataAsync(contract,  end, duration,
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
        contract.Symbol   = "EUR2";         // bad contract
        contract.SecType  = "CASH";
        contract.Currency = "USD";
        contract.Exchange = "IDEALPRO";

        client.TickByTickBidAskEvent += (s, e) =>
        {
            Debug.WriteLine(e.Arg2.Dump());
        };


        var ret = () => client.SubTickByTickData(contract, ETickByTickDataType.BidAsk).ConfigureAwait(false).GetAwaiter().GetResult();
        ret.Should().Throw<TwsException>();
    }


    [Test]
    public async Task SubTickByTickData_Test1()
    {
        Contract contract = new Contract();
        contract.Symbol   = "EUR";
        contract.SecType  = "CASH";
        contract.Currency = "USD";
        contract.Exchange = "IDEALPRO";

        client.TickByTickBidAskEvent += (s, e) =>
        {

            Debug.WriteLine(e.Arg2.Dump());

        };

        await client.SubTickByTickData(contract,  ETickByTickDataType.BidAsk);


        await Task.Delay(5000);
    }

#endregion

}
}