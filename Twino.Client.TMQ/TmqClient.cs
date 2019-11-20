using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Twino.Core;
using Twino.Protocols.TMQ;

namespace Twino.Client.TMQ
{
    public class TmqClient : ClientSocketBase<TmqMessage>
    {
        #region Connect - Read

        public override void Connect(DnsInfo host)
        {
            try
            {
                Client = new TcpClient();
                Client.Connect(host.IPAddress, host.Port);
                IsConnected = true;

                //creates SSL Stream or Insecure stream
                if (host.SSL)
                {
                    SslStream sslStream = new SslStream(Client.GetStream(), true, CertificateCallback);

                    X509Certificate2Collection certificates = null;
                    if (Certificate != null)
                    {
                        certificates = new X509Certificate2Collection();
                        certificates.Add(Certificate);
                    }

                    sslStream.AuthenticateAsClient(host.Hostname, certificates, false);
                    Stream = sslStream;
                }
                else
                    Stream = Client.GetStream();

                Stream.Write(PredefinedMessages.PROTOCOL_BYTES);

                //Reads the protocol response
                byte[] buffer = new byte[PredefinedMessages.PROTOCOL_BYTES.Length];
                int len = Stream.Read(buffer, 0, buffer.Length);

                CheckProtocolResponse(buffer, len);

                SendInfoMessage().Wait();
                Start();
            }
            catch
            {
                Disconnect();
                throw;
            }
        }

        public override async Task ConnectAsync(DnsInfo host)
        {
            try
            {
                Client = new TcpClient();
                await Client.ConnectAsync(host.IPAddress, host.Port);
                IsConnected = true;

                //creates SSL Stream or Insecure stream
                if (host.SSL)
                {
                    SslStream sslStream = new SslStream(Client.GetStream(), true, CertificateCallback);

                    X509Certificate2Collection certificates = null;
                    if (Certificate != null)
                    {
                        certificates = new X509Certificate2Collection();
                        certificates.Add(Certificate);
                    }

                    await sslStream.AuthenticateAsClientAsync(host.Hostname, certificates, false);
                    Stream = sslStream;
                }
                else
                    Stream = Client.GetStream();

                await Stream.WriteAsync(PredefinedMessages.PROTOCOL_BYTES);

                //Reads the protocol response
                byte[] buffer = new byte[PredefinedMessages.PROTOCOL_BYTES.Length];
                int len = await Stream.ReadAsync(buffer, 0, buffer.Length);

                CheckProtocolResponse(buffer, len);

                await SendInfoMessage();
                Start();
            }
            catch
            {
                Disconnect();
                throw;
            }
        }

        private static void CheckProtocolResponse(byte[] buffer, int len)
        {
            if (len < PredefinedMessages.PROTOCOL_BYTES.Length)
                throw new InvalidOperationException("Unexpected server response");

            for (int i = 0; i < PredefinedMessages.PROTOCOL_BYTES.Length; i++)
                if (PredefinedMessages.PROTOCOL_BYTES[i] != buffer[i])
                    throw new NotSupportedException("Unsupported TMQ Protocol version. Server supports: " + Encoding.UTF8.GetString(buffer));
        }

        private void Start()
        {
            //fire connected events and start to read data from the server until disconnected
            Thread thread = new Thread(async () =>
            {
                try
                {
                    while (IsConnected)
                        await Read();
                }
                catch
                {
                    Disconnect();
                }
            });

            thread.IsBackground = true;
            thread.Start();

            OnConnected();
        }

        private async Task SendInfoMessage()
        {
            throw new NotImplementedException();
        }

        protected override async Task Read()
        {
            TmqReader reader = new TmqReader();
            TmqMessage message = await reader.Read(Stream);
            if (message == null)
            {
                Disconnect();
                return;
            }

            switch (message.Type)
            {
                case MessageType.Terminate:
                    Disconnect();
                    break;

                case MessageType.Ping:
                    Pong();
                    break;

                default:
                    SetOnMessageReceived(message);
                    break;
            }
        }

        #endregion

        #region Ping - Pong

        public override void Ping()
        {
            Send(PredefinedMessages.PING);
        }

        public override void Pong()
        {
            Send(PredefinedMessages.PONG);
        }

        #endregion

        #region Send

        public bool Send(TmqMessage message)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> SendAsync(TmqMessage message)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}