namespace FTPServerWorkerRole 
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using Microsoft.WindowsAzure.ServiceRuntime;
    using AzureFtpServer.Azure;
    using AzureFtpServer.Ftp;
    using AzureFtpServer.Provider;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.Diagnostics;

    public class WorkerRole : RoleEntryPoint
    {
        private FtpServer _server;

        public override bool OnStart() {

            // Set the maximum number of concurrent connections, no use?
            //ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.
            RoleEnvironment.Changing += RoleEnvironmentChanging;

            Func<IPAddress> getLocalAddress = () => 
            {
                string ftpHost = RoleEnvironment.GetConfigurationSettingValue("FtpServerHost");

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
                            storageAccount: RoleEnvironment.GetConfigurationSettingValue("StorageAccount"),
                            sendQueueNotificationsOnUpload: bool.Parse(RoleEnvironment.GetConfigurationSettingValue("QueueNotification")),
                            accountInfo: RoleEnvironment.GetConfigurationSettingValue("FtpAccount")),
                        ftpEndpoint: RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["FTP"].IPEndpoint, 
                        pasvEndpoint: RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["FTPPASV"].IPEndpoint,
                        localAddress: getLocalAddress(),
                        maxClients: int.Parse(RoleEnvironment.GetConfigurationSettingValue("MaxClients")),
                        maxIdleSeconds: int.Parse(RoleEnvironment.GetConfigurationSettingValue("MaxIdleSeconds")),
                        connectionEncoding: RoleEnvironment.GetConfigurationSettingValue("ConnectionEncoding"));

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
}
