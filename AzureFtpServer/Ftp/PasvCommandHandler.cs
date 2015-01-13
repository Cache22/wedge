using System;
using System.Net;
using System.Net.Sockets;
using AzureFtpServer.Ftp;
using AzureFtpServer.General;

namespace AzureFtpServer.FtpCommands
{
    /// <summary>
    /// PASV command handler
    /// enter passive mode
    /// </summary>
    internal class PasvCommandHandler : FtpCommandHandler
    {
        private readonly IPEndPoint m_localPasvEndpoint;
        private readonly IPEndPoint m_externallyVisiblePasvEndpoint;

        // This command maybe won't work if the ftp server is deployed locally <= firewall
        public PasvCommandHandler(FtpConnectionObject connectionObject, IPEndPoint localPasvEndpoint, IPEndPoint externallyVisiblePasvEndpoint)
            : base("PASV", connectionObject)
        {
            if (externallyVisiblePasvEndpoint == null) throw new ArgumentNullException("localAddress", "The ftp server do not have a ipv4 address");

            // set passive listen port
            this.m_localPasvEndpoint = localPasvEndpoint;
            this.m_externallyVisiblePasvEndpoint = externallyVisiblePasvEndpoint;
        }

        protected override string OnProcess(string sMessage)
        {
            ConnectionObject.DataConnectionType = DataConnectionType.Passive;

            //return GetMessage(227, string.Format("Entering Passive Mode ({0})", pasvListenAddress));

            TcpListener listener = SocketHelpers.CreateTcpListener(this.m_localPasvEndpoint);
            if (listener == null)
            {
                return GetMessage(550, string.Format("Couldn't start listener on port {0}", this.m_localPasvEndpoint.Port));
            }

            string externallyVisibleAddress = this.m_externallyVisiblePasvEndpoint.AsAddressInfo();
            SocketHelpers.Send(ConnectionObject.Socket, string.Format("227 Entering Passive Mode ({0})\r\n", externallyVisibleAddress), ConnectionObject.Encoding);

            listener.Start();

            ConnectionObject.PassiveSocket = listener.AcceptTcpClient();

            listener.Stop();

            return "";
        }
    }

    public static class IPAddressExtensions
    {
        public static string AsAddressInfo(this IPEndPoint endpoint)
        {
            return endpoint.Address.ToString().Replace('.', ',')
                + ',' + (endpoint.Port / 256).ToString()
                + ',' + (endpoint.Port % 256).ToString();
        }
    }
}