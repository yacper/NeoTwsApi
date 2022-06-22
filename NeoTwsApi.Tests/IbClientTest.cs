using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using IBApi;
using NeoTwsApi.Enums;
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
    public async Task ReqHistoricalDataAsync_Test()
    {
        Contract contract = new Contract();
        contract.Symbol   = "EUR";
        contract.SecType  = "CASH";
        contract.Currency = "USD";
        contract.Exchange = "IDEALPRO";

        DateTime begin = 13.March(2022).At(00, 00);
        DateTime end = 17.March(2022).At(23, 59);

        var ret = await client.ReqHistoricalDataAsync(contract, begin, end,
                                                      ETimeFrameTws.D1, EDataType.MIDPOINT);
        Debug.WriteLine(ret.Dump());

        // Assert
        ret.First().Should().NotBeNull();

    }

#endregion
}
}