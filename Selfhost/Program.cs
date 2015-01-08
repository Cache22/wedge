namespace Selfhost
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using AzureFtpServer.Azure;
    using AzureFtpServer.Ftp;

    class Program
    {
        static void Main(string[] args)
        {
             var users = new Dictionary<string, string>();
            
             var _server = new FtpServer(fileSystemClassFactory: new AzureFileSystemFactory(
                     storageAccount: "UseDevelopmentStorage=true",
                     sendQueueNotificationsOnUpload: false,
                     checkAccount: (u, p) => users.ContainsKey(u) && users[u] == p),
                 ftpEndpoint: new IPEndPoint(IPAddress.Loopback, 21),
                 pasvEndpoint: new IPEndPoint(IPAddress.Loopback, 59860),
                 localAddress: IPAddress.Loopback,
                 maxClients: 5,
                 maxIdleTime: TimeSpan.FromMinutes(5),
                 connectionEncoding: "UTF8");

            _server.NewConnection += (nId) => Console.WriteLine("Connection {0} accepted", nId);

            users.Add("container1", "test1");
            users.Add("$root", "testroot");

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
                    Console.WriteLine("Server starting.");
                }
            }
        }
    }
}