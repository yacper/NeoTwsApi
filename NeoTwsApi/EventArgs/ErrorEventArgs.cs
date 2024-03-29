﻿// Licensed under the Apache License, Version 2.0.

namespace NeoTwsApi.EventArgs;

/// <summary>
/// The event arguments for Error events sent from TWS.
/// </summary>
public class ErrorEventArgs
{
    public override string ToString() => $"[{Id}]-[{ErrorCode}]{ErrorMessage}";

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorEventArgs"/> class.
    /// </summary>
    /// <param name="id">The error Id</param>
    /// <param name="errorCode">The error code</param>
    /// <param name="errorMsg">The error message</param>
    public ErrorEventArgs(int id, int errorCode, string errorMsg)
    {
        this.Id           = id;
        this.ErrorCode    = errorCode;
        this.ErrorMessage = errorMsg;
    }

    /// <summary>
    /// Gets the Id
    /// </summary>
    public int Id { get; private set; }

    /// <summary>
    /// Gets the error code
    /// </summary>
    public int ErrorCode { get; private set; }

    /// <summary>
    /// Gets the error message
    /// </summary>
    public string ErrorMessage { get; private set; }
}