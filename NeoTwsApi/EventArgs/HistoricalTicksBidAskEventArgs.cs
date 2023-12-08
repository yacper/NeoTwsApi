// Licensed under the Apache License, Version 2.0.

namespace NeoTwsApi.EventArgs
{
    using IBApi;

    /// <summary>
    /// The event arguments when a historical ticks is received
    /// </summary>
    public class HistoricalTicksBidAskEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HistoricalTicksBidAskEventArgs"/> class.
        /// </summary>
        /// <param name="reqId">The request identifier.</param>
        /// <param name="ticks">The historical ticks.</param>
        /// <param name="done">True if all data has been received. Otherwise false.</param>
        public HistoricalTicksBidAskEventArgs(int reqId, Contract reqContract,  HistoricalTickBidAsk[] ticks, bool done)
        {
            this.RequestId = reqId;
            this.Ticks     = ticks;
            this.Done      = done;
            this.ReqContract  = reqContract;
        }

        /// <summary>
        /// Gets the request id.
        /// </summary>
        public int RequestId { get; private set; }

        /// <summary>
        ///  请求的contract， 如果有
        /// </summary>
        public Contract ReqContract { get; protected set; }

        /// <summary>
        /// Gets the historical ticks.
        /// </summary>
        public HistoricalTickBidAsk[] Ticks { get; private set; }

        /// <summary>
        /// Gets a value indicating whether true if all data has been received. Otherwise false.
        /// </summary>
        public bool Done { get; private set; }
    }
}
