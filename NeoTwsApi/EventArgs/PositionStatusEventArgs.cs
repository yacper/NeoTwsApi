// Licensed under the Apache License, Version 2.0.

namespace NeoTwsApi.EventArgs
{
using IBApi;

/// <summary>
/// Event arguments for calls to the TWS Position APi.
/// </summary>
public class PositionStatusEventArgs
{
    public override string ToString() => $"{Account} {Contract} {Position}@{AverageCost}";

    /// <summary>
    /// Initializes a new instance of the <see cref="PositionStatusEventArgs"/> class.
    /// </summary>
    /// <param name="account">The account</param>
    /// <param name="contract">The contract</param>
    /// <param name="position">The position quantity</param>
    /// <param name="averageCost">The average cost</param>
    public PositionStatusEventArgs(string account, Contract contract, decimal position, double averageCost)
    {
        this.Account     = account;
        this.Contract    = contract;
        this.Position    = position;
        this.AverageCost = averageCost;
    }

    /// <summary>
    /// Gets the account
    /// </summary>
    public string Account { get; private set; }

    /// <summary>
    /// Gets the Contract
    /// </summary>
    public Contract Contract { get; private set; }

    /// <summary>
    /// Gets the position
    /// </summary>
    public decimal Position { get; private set; }

    /// <summary>
    /// Gets the average cost, 这个平均成本不是平均价格，是包含了手续费的真实成本
    /// </summary>
    public double AverageCost { get; private set; }
}
}