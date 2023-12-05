// created: 2022/06/28 16:50
// author:  rush
// email:   yacper@gmail.com
// 
// purpose:
// modifiers:

using System.Collections.Generic;
using IBApi;
using NeoTwsApi.Enums;

namespace NeoTwsApi.Tests;

public partial class Tests
{
    Contract MsftContract = new Contract
    {
        SecType     = ESecTypeTws.STK.ToString(),
        Symbol      = "MSFT",
        Exchange    = EExchangeTws.SMART.ToString(),
        PrimaryExch = EExchangeTws.NYSE.ToString(),
        Currency    = ECurrencyTws.USD.ToString()
    };

    // aapl明明在nasdaq上市，但是这里不能用nasdaq，只能用nyse，或者smart路由
    Contract AaplContract = new Contract
    {
        SecType     = ESecTypeTws.STK.ToString(),
        Symbol      = "AAPL",
        //Exchange    = EExchangeTws.NASDAQ.ToString(), // 这里不能用nasdaq， ib认为aapl在nyse上市
        Exchange    = EExchangeTws.SMART.ToString(), // 可以用smart
        //Exchange    = EExchangeTws.NYSE.ToString(), // 或者nyse
        //PrimaryExch = EExchangeTws.NYSE.ToString(),
        Currency    = ECurrencyTws.USD.ToString()
    };

    private Contract EurContract = new Contract()
    {
        Symbol   = "EUR",
        SecType  = ESecTypeTws.CASH.ToString(),
        Currency = ECurrencyTws.USD.ToString(),
        Exchange = EExchangeTws.IDEALPRO.ToString()
    };

    public Contract XauusdContract_CMDTY = new Contract()
    {
        Symbol   = "XAUUSD",
        SecType  = ESecTypeTws.CMDTY.ToString(),
        Exchange = EExchangeTws.SMART.ToString(),
        Currency = ECurrencyTws.USD.ToString()
    };

    public Contract QQQContract_ETF = new Contract()
    {
        Symbol   = "QQQ",
        SecType  = ESecTypeTws.STK.ToString(),
        Exchange = EExchangeTws.SMART.ToString(),
        Currency = ECurrencyTws.USD.ToString()
    };

    /*
     * STK Combo contract
     * Leg 1: 43645865 - IBKR's STK
     * Leg 2: 9408 - McDonald's STK
     */
    public static Contract StockComboContract()
    {
        //! [bagstkcontract]
        Contract contract = new Contract();
        contract.Symbol   = "IBKR,MCD";
        contract.SecType  = "BAG";
        contract.Currency = "USD";
        contract.Exchange = "SMART";

        ComboLeg leg1 = new ComboLeg();
        leg1.ConId    = 43645865; //IBKR STK
        leg1.Ratio    = 1;
        leg1.Action   = "BUY";
        leg1.Exchange = "SMART";

        ComboLeg leg2 = new ComboLeg();
        leg2.ConId    = 9408; //MCD STK
        leg2.Ratio    = 1;
        leg2.Action   = "SELL";
        leg2.Exchange = "SMART";

        contract.ComboLegs = new List<ComboLeg>();
        contract.ComboLegs.Add(leg1);
        contract.ComboLegs.Add(leg2);
        //! [bagstkcontract]
        return contract;
    }
}