namespace Selfhost
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using AzureFtpServer.Azure;
    using AzureFtpServer.Ftp;
    using System.Diagnostics;
    using System.Net.Sockets;
    using System.IO;

    class Program
    {
        static void Main(string[] args)
        {
            Func<string, string> cfg = key => System.Configuration.ConfigurationManager.AppSettings[key];
            Func<string, IPEndPoint> endpoint = name => {
                string epr = cfg(name + "_ENDPOINT");

                var host = epr.Substring(0, epr.IndexOf(":"));
                var port = epr.Substring(epr.IndexOf(":")  + 1);
                return new IPEndPoint(IPAddress.Parse(host), port.ToInt());
            };

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

            Func<Func<string, string, bool>> CheckAccount = () =>
            {
                var json = File.ReadAllText(cfg("userfile"));
                var users = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                return (u, p) => users.ContainsKey(u) && users[u] == p;
            };

            var _server = new FtpServer(
                fileSystemClassFactory: new AzureFileSystemFactory(
                        storageAccount: cfg("StorageAccount"),
                        sendQueueNotificationsOnUpload: bool.Parse(cfg("QueueNotification")),
                        checkAccount: CheckAccount()),
                    ftpEndpoint: endpoint("FTP"),
                    pasvEndpoint: endpoint("FTPPASV"),
                    localAddress: getLocalAddress(),
                    maxClients: cfg("MaxClients").ToInt(),
                    maxIdleTime: TimeSpan.FromSeconds(cfg("MaxIdleSeconds").ToInt()),
                    connectionEncoding: cfg("ConnectionEncoding"));

            // var users = new Dictionary<string, string>();
            //var _server = new FtpServer(fileSystemClassFactory: new AzureFileSystemFactory(
            //        storageAccount: "UseDevelopmentStorage=true",
            //        sendQueueNotificationsOnUpload: false,
            //        checkAccount: (u, p) => users.ContainsKey(u) && users[u] == p),
            //    ftpEndpoint: new IPEndPoint(IPAddress.Loopback, 21),
            //    pasvEndpoint: new IPEndPoint(IPAddress.Loopback, 59860),
            //    localAddress: IPAddress.Loopback,
            //    maxClients: 5,
            //    maxIdleTime: TimeSpan.FromMinutes(5),
            //    connectionEncoding: "UTF8");
            //users.Add("user", "pass");
            //users.Add("$root", "testroot");

            _server.NewConnection += (nId) => Console.WriteLine("Connection {0} accepted", nId);

            while (true)
            {
                if (!_server.Started)
                {
                    _server.Start();
                    Console.WriteLine("Server starting.");
                }
                else
                {
                    Thread.Sleep(TimeSpan.FromSeconds(10));
                    Console.WriteLine("Server running.");
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