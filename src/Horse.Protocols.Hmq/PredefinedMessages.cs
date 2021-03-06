using System.Text;

namespace Horse.Protocols.Hmq
{
    /// <summary>
    /// Predefined messages for HMQ Protocol
    /// </summary>
    public static class PredefinedMessages
    {
        /// <summary>
        /// "HMQP/2.1" as bytes, protocol handshaking message
        /// </summary>
        public static readonly byte[] PROTOCOL_BYTES_V2 = Encoding.ASCII.GetBytes("HMQP/2.1");

        /// <summary>
        /// PING message for HMQ "0x89, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00"
        /// </summary>
        public static readonly byte[] PING = { 0x89, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        /// <summary>
        /// PONG message for HMQ "0x8A, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00"
        /// </summary>
        public static readonly byte[] PONG = { 0x8A, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
    }
}