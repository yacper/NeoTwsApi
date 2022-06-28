// created: 2022/06/28 17:59
// author:  rush
// email:   yacper@gmail.com
// 
// purpose: only list common properties
// modifiers:

using System.Collections.Concurrent;
using NeoTwsApi.Enums;

namespace NeoTwsApi.Models;

public class AccountDetails:ConcurrentDictionary<string, string>
{
    public string AccountCode => this.ContainsKey(nameof(AccountCode)) ? this[nameof(BuyingPower)] : null;
    public string AccountType => this.ContainsKey(nameof(AccountType)) ? this[nameof(AccountType)] : null;
    public ECurrencyTws? Currency => this.ContainsKey(nameof(Currency)) ? Enum.Parse<ECurrencyTws>(this[nameof(Currency)]) : null;


    public double? BuyingPower => this.ContainsKey(nameof(BuyingPower)) ? Convert.ToDouble(this[nameof(BuyingPower)]) : null;
    public double? TotalCashValue => this.ContainsKey(nameof(TotalCashValue)) ? Convert.ToDouble(this[nameof(TotalCashValue)]) : null;
    public double? GrossPositionValue => this.ContainsKey(nameof(GrossPositionValue)) ? Convert.ToDouble(this[nameof(GrossPositionValue)]) : null;
    public double? StockMarketValue => this.ContainsKey(nameof(StockMarketValue)) ? Convert.ToDouble(this[nameof(StockMarketValue)]) : null;

    public double? InitMarginReq => this.ContainsKey(nameof(InitMarginReq)) ? Convert.ToDouble(this[nameof(InitMarginReq)]) : null;
    public double? MaintMarginReq => this.ContainsKey(nameof(MaintMarginReq)) ? Convert.ToDouble(this[nameof(MaintMarginReq)]) : null;

    public double? DayTradesRemaining => this.ContainsKey(nameof(DayTradesRemaining)) ? Convert.ToDouble(this[nameof(DayTradesRemaining)]) : null;



    /* possible kvs
    {
        "MutualFundValue", "0.00"
    }
    {
        "FullExcessLiquidity-C", "0.00"
    }
    {
        "ColumnPrio-S", "1"
    }
    {
        "AccountCode", "DU5761723"
    }
    {
        "EquityWithLoanValue-S", "1000011.61"
    }
    {
        "PASharesValue-S", "0.00"
    }
    {
        "RealizedPnL", "0.00"
    }
    {
        "ColumnPrio-C", "2"
    }
    {
        "NetLiquidation-S", "1000011.61"
    }
    {
        "ExcessLiquidity-S", "999772.42"
    }
    {
        "PreviousDayEquityWithLoanValue", "1000066.65"
    }
    {
        "LookAheadAvailableFunds-C", "0.00"
    }
    {
        "TotalCashValue-S", "999006.40"
    }
    {
        "EquityWithLoanValue-C", "0.00"
    }
    {
        "PhysicalCertificateValue-S", "0.00"
    }
    {
        "FxCashBalance", "0.00"
    }
    {
        "AccruedDividend-S", "0.00"
    }
    {
        "FullAvailableFunds-S", "999748.50"
    }
    {
        "GrossPositionValue", "797.31"
    }
    {
        "AccruedCash", "207.90"
    }
    {
        "RegTMargin", "398.65"
    }
    {
        "LookAheadAvailableFunds", "999748.50"
    }
    {
        "TBillValue", "0.00"
    }
    {
        "FullExcessLiquidity-S", "999772.42"
    }
    {
        "PostExpirationMargin-S", "0.00"
    }
    {
        "IndianStockHaircut-C", "0.00"
    }
    {
        "AccountOrGroup", "DU5761723"
    }
    {
        "GrossPositionValue-S", "797.31"
    }
    {
        "LookAheadInitMarginReq-C", "0.00"
    }
    {
        "TotalDebitCardPendingCharges-C", "0.00"
    }
    {
        "AccountType", "INDIVIDUAL"
    }
    {
        "TradingType-S", "STKNOPT"
    }
    {
        "NetDividend", "0.00"
    }
    {
        "Guarantee", "0.00"
    }
    {
        "LookAheadMaintMarginReq-C", "0.00"
    }
    {
        "StockMarketValue", "797.31"
    }
    {
        "IndianStockHaircut", "0.00"
    }
    {
        "LookAheadExcessLiquidity-S", "999772.42"
    }
    {
        "PhysicalCertificateValue-C", "0.00"
    }
    {
        "Currency", "USD"
    }
    {
        "RegTMargin-S", "398.65"
    }
    {
        "TotalCashValue", "999006.40"
    }
    {
        "LookAheadExcessLiquidity-C", "0.00"
    }
    {
        "PASharesValue-C", "0.00"
    }
    {
        "LookAheadMaintMarginReq-S", "239.19"
    }
    {
        "IndianStockHaircut-S", "0.00"
    }
    {
        "FullInitMarginReq-S", "263.11"
    }
    {
        "Guarantee-C", "0.00"
    }
    {
        "LookAheadNextChange", "0"
    }
    {
        "AvailableFunds-S", "999748.50"
    }
    {
        "FullAvailableFunds-C", "0.00"
    }
    {
        "TotalCashValue-C", "0.00"
    }
    {
        "DayTradesRemainingT+2", "-1"
    }
    {
        "MoneyMarketFundValue", "0.00"
    }
    {
        "NetLiquidationUncertainty", "0.00"
    }
    {
        "RegTEquity-S", "1000011.61"
    }
    {
        "Cushion", "0.999761"
    }
    {
        "SMA-S", "999680.92"
    }
    {
        "FundValue", "0.00"
    }
    {
        "TotalDebitCardPendingCharges", "0.00"
    }
    {
        "PhysicalCertificateValue", "0.00"
    }
    {
        "AvailableFunds", "999748.50"
    }
    {
        "AccruedCash-C", "0.00"
    }
    {
        "PostExpirationExcess-S", "0.00"
    }
    {
        "FullExcessLiquidity", "999772.42"
    }
    {
        "LookAheadInitMarginReq", "263.11"
    }
    {
        "DayTradesRemaining", "-1"
    }
    {
        "AccruedDividend", "0.00"
    }
    {
        "TotalCashBalance", "867730.70"
    }
    {
        "DayTradesRemainingT+1", "-1"
    }
    {
        "SegmentTitle-S", "US Securities"
    }
    {
        "MaintMarginReq-C", "0.00"
    }
    {
        "FutureOptionValue", "0.00"
    }
    {
        "AccountReady", "true"
    }
    {
        "CorporateBondValue", "0.00"
    }
    {
        "RealCurrency", "USD"
    }
    {
        "RegTEquity", "1000011.61"
    }
    {
        "LookAheadMaintMarginReq", "239.19"
    }
    {
        "WarrantValue", "0.00"
    }
    {
        "InitMarginReq-C", "0.00"
    }
    {
        "LookAheadAvailableFunds-S", "999748.50"
    }
    {
        "PostExpirationExcess-C", "0.00"
    }
    {
        "NetLiquidationByCurrency", "868735.91"
    }
    {
        "TBondValue", "0.00"
    }
    {
        "MaintMarginReq", "239.19"
    }
    {
        "LookAheadExcessLiquidity", "999772.42"
    }
    {
        "SMA", "999680.92"
    }
    {
        "UnrealizedPnL", "-12.69"
    }
    {
        "InitMarginReq-S", "263.11"
    }
    {
        "FuturesPNL", "0.00"
    }
    {
        "LookAheadInitMarginReq-S", "263.11"
    }
    {
        "InitMarginReq", "263.11"
    }
    {
        "PostExpirationMargin-C", "0.00"
    }
    {
        "MaintMarginReq-S", "239.19"
    }
    {
        "FullInitMarginReq", "263.11"
    }
    {
        "DayTradesRemainingT+4", "-1"
    }
    {
        "CashBalance", "867730.70"
    }
    {
        "WhatIfPMEnabled", "true"
    }
    {
        "FullMaintMarginReq-S", "239.19"
    }
    {
        "AccruedCash-S", "207.90"
    }
    {
        "DayTradesRemainingT+3", "-1"
    }
    {
        "AccruedDividend-C", "0.00"
    }
    {
        "Billable-C", "0.00"
    }
    {
        "AvailableFunds-C", "0.00"
    }
    {
        "ExcessLiquidity", "999772.42"
    }
    {
        "NetLiquidation", "1000011.61"
    }
    {
        "IssuerOptionValue", "0.00"
    }
    {
        "ExcessLiquidity-C", "0.00"
    }
    {
        "SegmentTitle-C", "US Commodities"
    }
    {
        "PreviousDayEquityWithLoanValue-S", "1000066.65"
    }
    {
        "Leverage-S", "0.00"
    }
    {
        "Billable", "0.00"
    }
    {
        "FullMaintMarginReq-C", "0.00"
    }
    {
        "Guarantee-S", "0.00"
    }
    {
        "BuyingPower", "3998993.99"
    }
    {
        "PostExpirationExcess", "0.00"
    }
    {
        "FullInitMarginReq-C", "0.00"
    }
    {
        "FullMaintMarginReq", "239.19"
    }
    {
        "EquityWithLoanValue", "1000011.61"
    }
    {
        "FullAvailableFunds", "999748.50"
    }
    {
        "PostExpirationMargin", "0.00"
    }
    {
        "NetLiquidation-C", "0.00"
    }
    {
        "TotalDebitCardPendingCharges-S", "0.00"
    }
    {
        "NLVAndMarginInReview", "false"
    }
    {
        "OptionMarketValue", "0.00"
    }
    {
        "ExchangeRate", "1.00"
    }
    {
        "PASharesValue", "0.00"
    }
    {
        "Billable-S", "0.00"
    }
    */
}