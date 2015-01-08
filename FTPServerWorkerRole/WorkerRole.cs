namespace FTPServerWorkerRole 
{
    using AzureFtpServer.Azure;
    using AzureFtpServer.Ftp;
    using AzureFtpServer.Provider;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.Diagnostics;
    using Microsoft.WindowsAzure.ServiceRuntime;
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;

    public class WorkerRole : RoleEntryPoint
    {
        private FtpServer _server;

        public override bool OnStart() {

            // Set the maximum number of concurrent connections, no use?
            //ServicePointManager.DefaultConnectionLimit = 12;

            RoleEnvironment.Changing += RoleEnvironmentChanging;

            Func<string, string> cfg = RoleEnvironment.GetConfigurationSettingValue;
            Func<string, IPEndPoint> endpoint = name => RoleEnvironment.CurrentRoleInstance.InstanceEndpoints[name].IPEndpoint;
            Func<IPAddress> getLocalAddress = () => 
            {
                string ftpHost = cfg("FtpServerHost");

                if (ftpHost.ToLower() == "localhost") 
                    return IPAddress.Loopback;

                foreach (var ip in Dns.GetHostEntry(ftpHost).AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        Trace.TraceInformation(string.Format("localAddress == {0}", ip));
                        return ip;
                    }
                }

                return IPAddress.None;
            };

            if (_server == null)
                _server = new FtpServer(
                    fileSystemClassFactory: new AzureFileSystemFactory(
                            storageAccount: cfg("StorageAccount"),
                            sendQueueNotificationsOnUpload: bool.Parse(cfg("QueueNotification")),
                            accounts: AccountManager.ParseOldConfiguration(cfg("FtpAccount"))),
                        ftpEndpoint: endpoint("FTP"),
                        pasvEndpoint: endpoint("FTPPASV"),
                        localAddress: getLocalAddress(),
                        maxClients: cfg("MaxClients").ToInt(),
                        maxIdleTime: TimeSpan.FromSeconds(cfg("MaxIdleSeconds").ToInt()),
                        connectionEncoding: cfg("ConnectionEncoding"));

            _server.NewConnection += ServerNewConnection;

            return base.OnStart();
        }

        static void ServerNewConnection(int nId) {
            Trace.WriteLine(String.Format("Connection {0} accepted", nId), "Connection");
        }

        private static void RoleEnvironmentChanging(object sender, RoleEnvironmentChangingEventArgs e) {
            // If a configuration setting is changing
            if (e.Changes.Any(change => change is RoleEnvironmentConfigurationSettingChange)) {
                // Set e.Cancel to true to restart this role instance
                e.Cancel = true;
            }
        }

        public override void Run()
        {

            // This is a sample worker implementation. Replace with your logic.
            Trace.WriteLine("FTPRole entry point called", "Information");

            while (true)
            {
                if (_server.Started)
                {
                    Thread.Sleep(10000);
                    //Trace.WriteLine("Server is alive.", "Information");
                }
                else
                {
                    _server.Start();
                    Trace.WriteLine("Server starting.", "Control");
                }
            }
        }
    }

    public static class ParsingExtensions
    {
        public static bool ToBool(this string value) { return bool.Parse(value); }
        public static int ToInt(this string value) { return int.Parse(value); }
    }
}
