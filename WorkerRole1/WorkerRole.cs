namespace FTPServerRole 
{
    using AzureFtpServer.Azure;
    using AzureFtpServer.Ftp;
    using AzureFtpServer.Provider;
    using Microsoft.WindowsAzure.ServiceRuntime;
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;

    public class WorkerRole : RoleEntryPoint
    {
        private FtpServer _server;

        public override bool OnStart() {

            // Set the maximum number of concurrent connections, no use?
            //ServicePointManager.DefaultConnectionLimit = 12;

            StorageProviderConfiguration.Mode = (Modes)Enum.Parse(typeof(Modes), RoleEnvironment.GetConfigurationSettingValue("Mode"));
            StorageProviderConfiguration.FtpAccount = RoleEnvironment.GetConfigurationSettingValue("FtpAccount");
            StorageProviderConfiguration.FtpServerHost = RoleEnvironment.GetConfigurationSettingValue("FtpServerHost");

            if (StorageProviderConfiguration.Mode == Modes.Live)
                ConfigureDiagnosticsV1_4();

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.
            RoleEnvironment.Changing += RoleEnvironmentChanging;

            if (_server == null)
                _server = new FtpServer(
                    fileSystemClassFactory: new AzureFileSystemFactory(
                            storageAccount: RoleEnvironment.GetConfigurationSettingValue("StorageAccount"),
                            sendQueueNotificationsOnUpload: bool.Parse(RoleEnvironment.GetConfigurationSettingValue("QueueNotification"))),
                        ftpEndpoint: RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["FTP"].IPEndpoint, 
                        pasvEndpoint: RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["FTPPASV"].IPEndpoint,
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

        private static void ConfigureDiagnosticsV1_4()
        {
            //DiagnosticMonitorTraceListener tmpListener = new DiagnosticMonitorTraceListener();
            //Trace.Listeners.Add(tmpListener);

            //CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(
            //    RoleEnvironment.GetConfigurationSettingValue("Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString"));

            //var manager = cloudStorageAccount.CreateRoleInstanceDiagnosticManager(
            //    RoleEnvironment.DeploymentId,
            //    RoleEnvironment.CurrentRoleInstance.Role.Name,
            //    RoleEnvironment.CurrentRoleInstance.Id);

            //DiagnosticMonitorConfiguration config = manager.GetCurrentConfiguration();

            //config.ConfigurationChangePollInterval = TimeSpan.FromMinutes(1);

            //config.Logs.ScheduledTransferPeriod = TimeSpan.FromMinutes(1D);
            //config.Logs.ScheduledTransferLogLevelFilter = LogLevel.Undefined;

            //config.Directories.ScheduledTransferPeriod = TimeSpan.FromMinutes(5);
            
            //config.WindowsEventLog.DataSources.Add("Application!*");
            //config.WindowsEventLog.DataSources.Add("System!*");
            //config.WindowsEventLog.ScheduledTransferPeriod = TimeSpan.FromMinutes(5);

            //PerformanceCounterConfiguration performanceCounterConfiguration = new PerformanceCounterConfiguration();
            //performanceCounterConfiguration.CounterSpecifier = @"\Processor(_Total)\% Processor Time";
            //performanceCounterConfiguration.SampleRate = TimeSpan.FromSeconds(10d);

            //config.PerformanceCounters.DataSources.Add(performanceCounterConfiguration);
            //config.PerformanceCounters.ScheduledTransferPeriod = TimeSpan.FromMinutes(1d);

            //manager.SetCurrentConfiguration(config);
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
