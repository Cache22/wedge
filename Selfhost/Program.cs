namespace Selfhost
{
    using AzureFtpServer.Azure;
    using AzureFtpServer.Ftp;
    using System;
    using System.Net;
    using System.Threading;

    class Program
    {
        static void Main(string[] args)
        {
            var _server = new FtpServer(fileSystemClassFactory: new AzureFileSystemFactory(
                    storageAccount: "UseDevelopmentStorage=true",
                    sendQueueNotificationsOnUpload: false,
                    accountInfo: "(container1:test1)(container2:test2)(container3:test3)($root:testroot)"),
                ftpEndpoint: new IPEndPoint(IPAddress.Loopback, 21),
                pasvEndpoint: new IPEndPoint(IPAddress.Loopback, 59860),
                localAddress: IPAddress.Loopback,
                maxClients: 5,
                maxIdleTime: TimeSpan.FromSeconds(5),
                connectionEncoding: "UTF8");

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
                    Console.WriteLine("Server starting.");
                }

            }
        }
    }
}
