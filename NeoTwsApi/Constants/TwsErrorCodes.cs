// Licensed under the Apache License, Version 2.0.

namespace NeoTwsApi.Constants
{
    /// <summary>
    /// The class that holds the error codes for TWS
    /// </summary>
    public class TwsErrorCodes
    {
        /// <summary>
        /// Ambiguous contract error code
        /// </summary>
        public const int AmbiguousContract = 200;

        /// <summary>
        /// An attempted order was rejected by the IB servers.
        /// </summary>
        public const int OrderRejected = 201;

        /// <summary>
        /// OrderCancelled error code
        /// </summary>
        public const int OrderCancelled = 202;

        /// <summary>
        /// Server error when reading an API client request.
        /// </summary>
        public const int ServerErrorWhenReadingApiClientRequest = 320;

        /// <summary>
        /// Server error when validating an API client request.
        /// </summary>
        public const int ServerErrorWhenValidatingApiClientRequest = 321;

        /// <summary>
        /// Server error when validating an API client request.
        /// </summary>
        public const int ServerErrorWhenProcessingApiClientRequest = 322;

        /// <summary>
        /// Invalid order type error code
        /// </summary>
        public const int InvalidOrderType = 387;

        /// <summary>
        /// End Time: The date, time, or time-zone entered is invalid. The correct format is yyyymmdd hh:mm:ss xx/xxxx where yyyymmdd and xx/xxxx are optional. E.g.: 20031126 15:59:00 US/Eastern  Note that there is a space between the date and time, and between the time and time-zone.  If no date is specified, current date is assumed. If no time-zone is specified, local time-zone is assumed(deprecated).  You can also provide yyyymmddd-hh:mm:ss time is in UTC. Note that there is a dash between the date and time in UTC notation
        /// 当设置tif错误的时候，会返回这个错误
        /// </summary>
        public const int InvalidEndTime = 391;

        /// <summary>
        /// Order message error (i.e Your order was repriced)
        /// </summary>
        public const int OrderMessageError = 399;

        /// <summary>
        /// Order can't be cancelled (possibly filled)
        /// </summary>
        public const int OrderCannotBeCancelled2 = 10147;

        /// <summary>
        /// Order can't be cancelled (possibly filled)
        /// </summary>
        public const int OrderCannotBeCancelled = 10148;

        /// <summary>
        /// This order doesn't support fractional quantity trading
        /// 不支持散弹
        /// </summary>
        public const int OrderNotSupportFractionalQuantity = 10318;

    }
}
