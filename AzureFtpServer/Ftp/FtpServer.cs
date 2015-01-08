using System;
using System.Collections;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Diagnostics;
using AzureFtpServer.Ftp.FileSystem;
using AzureFtpServer.General;
using AzureFtpServer.Provider;
using System.Net;

namespace AzureFtpServer.Ftp
{
    /// <summary>
    /// Listens for incoming connections and accepts them.
    /// Incomming socket connections are then passed to the socket handling class (FtpSocketHandler).
    /// </summary>
    public class FtpServer
    {
        #region Member Variables

        private readonly ArrayList m_apConnections;
        private readonly IFileSystemClassFactory m_fileSystemClassFactory;
        private int m_nId;
        private TcpListener m_socketListen;
        private Thread m_theThread;
        private bool m_started = false;
        private Encoding m_encoding;
        private int m_maxClients;
        private IPEndPoint m_pasvEndpoint;
        private IPEndPoint m_ftpEndpoint;
        private IPAddress m_localAddress;
        private int m_maxIdleSeconds;

        #endregion

        #region Events

        #region Delegates

        public delegate void ConnectionHandler(int nId);

        #endregion

        public event ConnectionHandler ConnectionClosed;
        public event ConnectionHandler NewConnection;

        #endregion

        #region Construction

        public FtpServer(IFileSystemClassFactory fileSystemClassFactory, IPEndPoint ftpEndpoint, IPEndPoint pasvEndpoint, 
            IPAddress localAddress, int maxClients, int maxIdleSeconds, string connectionEncoding)
        {
            m_apConnections = new ArrayList();
            m_fileSystemClassFactory = fileSystemClassFactory;
            this.m_ftpEndpoint = ftpEndpoint;
            this.m_pasvEndpoint = pasvEndpoint;
            this.m_localAddress = localAddress;
            this.m_maxClients = maxClients;
            this.m_maxIdleSeconds = maxIdleSeconds;

            switch (connectionEncoding)
            {
                case "ASCII":
                    m_encoding = Encoding.ASCII;
                    Trace.WriteLine("Set ftp connection encoding: ASCII", "Information");
                    break;
                case "UTF8":
                default:
                    m_encoding = Encoding.UTF8;
                    Trace.WriteLine("Set ftp connection encoding: UTF8", "Information");
                    break;
            }
        }

        ~FtpServer()
        {
            if (m_socketListen != null)
            {
                m_socketListen.Stop();
            }
        }

        public bool Started
        {
            get { return m_started; }
        }

        #endregion

        #region Methods

        public void Start()
        {
            m_theThread = new Thread(ThreadRun);
            m_theThread.Start();
            m_started= true;
        }

        public void Stop()
        {
            for (int nConnection = 0; nConnection < m_apConnections.Count; nConnection++)
            {
                var handler = m_apConnections[nConnection] as FtpSocketHandler;
                handler.Stop();
            }

            m_socketListen.Stop();
            m_theThread.Join();
            m_started = false;
        }

        /// <summary>
        /// The main thread of the ftp server
        /// Listen and acception clients, create handler threads for each client
        /// </summary>
        private void ThreadRun()
        {
            FtpServerMessageHandler.Message += TraceMessage;

            // listen at the port by the "FTP" endpoint setting
            m_socketListen = SocketHelpers.CreateTcpListener(this.m_ftpEndpoint);

            if (m_socketListen != null)
            {
                Trace.TraceInformation("FTP Server listened at: " + this.m_ftpEndpoint);

                m_socketListen.Start();

                Trace.TraceInformation("FTP Server Started");

                bool fContinue = true;

                while (fContinue)
                {
                    TcpClient socket = null;

                    try
                    {
                        socket = m_socketListen.AcceptTcpClient();
                    }
                    catch (SocketException)
                    {
                        fContinue = false;
                    }
                    finally
                    {
                        if (socket == null)
                        {
                            fContinue = false;
                        }
                        else if (m_apConnections.Count >= m_maxClients)
                        {
                            Trace.WriteLine("Too many clients, won't handle this connection", "Warnning");
                            SendRejectMessage(socket);
                            socket.Close();
                        }
                        else
                        {
                            socket.NoDelay = false;

                            m_nId++;

                            FtpServerMessageHandler.SendMessage(m_nId, "New connection");

                            SendAcceptMessage(socket);
                            InitialiseSocketHandler(socket);
                        }
                    }
                }
            }
            else
            {
                FtpServerMessageHandler.SendMessage(0, "Error in starting FTP server");
            }
        }


        private void SendAcceptMessage(TcpClient socket)
        {
            SocketHelpers.Send(socket, m_encoding.GetBytes("220 FTP to Windows Azure Blob Storage Bridge Ready\r\n"));
        }

        private void SendRejectMessage(TcpClient socket)
        {
            SocketHelpers.Send(socket, m_encoding.GetBytes("421 Too many users now\r\n"));
        }

        private void InitialiseSocketHandler(TcpClient socket)
        {
            var handler = new FtpSocketHandler(m_fileSystemClassFactory, m_nId, 
                pasvEndpoint: this.m_pasvEndpoint, 
                localAddress: this.m_localAddress,
                maxIdleSeconds: this.m_maxIdleSeconds);
            
            // get encoding for the socket connection
            
            handler.Start(socket, m_encoding);

            m_apConnections.Add(handler);

            Trace.WriteLine(
                string.Format("Add a new handler, current connection number is {0}", m_apConnections.Count),
                "Information");

            handler.Closed += handler_Closed;

            if (NewConnection != null)
            {
                NewConnection(m_nId);
            }
        }

        #endregion

        #region Event Handlers

        private void handler_Closed(FtpSocketHandler handler)
        {
            m_apConnections.Remove(handler);

            Trace.WriteLine(
                string.Format("Remover a handler, current connection number is {0}", m_apConnections.Count),
                "Information");

            if (ConnectionClosed != null)
            {
                ConnectionClosed(handler.Id);
            }
        }

        public void TraceMessage(int nId, string sMessage)
        {
            Trace.WriteLine(string.Format("{0}: {1}", nId, sMessage), "FtpServerMessage");
        }

        #endregion
    }
}