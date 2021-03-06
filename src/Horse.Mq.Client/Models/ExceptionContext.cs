using System;
using Horse.Protocols.Hmq;

namespace Horse.Mq.Client.Models
{
    /// <summary>
    /// Used when an exception is ready to push or publish
    /// </summary>
    public class ExceptionContext
    {
        /// <summary>
        /// Consumer object
        /// </summary>
        public object Consumer { get; set; }
        
        /// <summary>
        /// The consuming message when exception is thrown
        /// </summary>
        public HorseMessage ConsumingMessage { get; set; }
        
        /// <summary>
        /// Exception object
        /// </summary>
        public Exception Exception { get; set; }
    }
}