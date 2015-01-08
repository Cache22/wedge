using System;
using System.Net;
using System.Net.Sockets;
using AzureFtpServer.Ftp;
using AzureFtpServer.General;
using AzureFtpServer.Provider;

namespace AzureFtpServer.FtpCommands
{
    /// <summary>
    /// PASV command handler
    /// enter passive mode
    /// </summary>
    internal class PasvCommandHandler : FtpCommandHandler
    {
        private readonly IPEndPoint m_pasvEndpoint;
        private readonly IPAddress m_localAddress;

        // This command maybe won't work if the ftp server is deployed locally <= firewall
        public PasvCommandHandler(FtpConnectionObject connectionObject, IPEndPoint pasvEndpoint, IPAddress localAddress)
            : base("PASV", connectionObject)
        {
            if (localAddress == null) throw new ArgumentNullException("localAddress", "The ftp server do not have a ipv4 address");

            // set passive listen port
            this.m_pasvEndpoint = pasvEndpoint;
            this.m_localAddress = localAddress;
        }

        protected override string OnProcess(string sMessage)
        {
            ConnectionObject.DataConnectionType = DataConnectionType.Passive;

            string pasvListenAddress = GetPassiveAddressInfo();

            //return GetMessage(227, string.Format("Entering Passive Mode ({0})", pasvListenAddress));



            TcpListener listener = SocketHelpers.CreateTcpListener(this.m_pasvEndpoint);

            if (listener == null)
            {
                return GetMessage(550, string.Format("Couldn't start listener on port {0}", this.m_pasvEndpoint.Port));
            }

            SocketHelpers.Send(ConnectionObject.Socket, string.Format("227 Entering Passive Mode ({0})\r\n", pasvListenAddress), ConnectionObject.Encoding);

            listener.Start();

            ConnectionObject.PassiveSocket = listener.AcceptTcpClient();

            listener.Stop();

            return "";
        }

        private string GetPassiveAddressInfo()
        {
            // get local ipv4 ip
            IPAddress ipAddress = this.m_localAddress;
            string retIpPort = ipAddress.ToString();
            retIpPort = retIpPort.Replace('.', ',');

            // append the port
            retIpPort += ',';
            retIpPort += (this.m_pasvEndpoint.Port / 256).ToString();
            retIpPort += ',';
            retIpPort += (this.m_pasvEndpoint.Port % 256).ToString();

            return retIpPort;
        }

       
    }
}