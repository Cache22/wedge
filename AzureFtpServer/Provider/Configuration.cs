namespace AzureFtpServer.Provider
{
    using System;
    using System.Configuration;

    public enum Modes
    {
        Live,
        Debug
    }

    public static class StorageProviderConfiguration
    {
        public static string FtpAccount { get; set; }
        public static Modes Mode { get; set; }
        public static bool QueueNotification { get; set; }
        public static int MaxIdleSeconds { get; set; }
        public static string FtpServerHost { get; set; }

        public static System.Net.IPEndPoint FTPPASVEndpoint { get; set; }

        public static System.Net.IPEndPoint FTPEndpoint { get; set; }

        public static string ConnectionEncoding { get; set; }

        public static string MaxClients { get; set; }
    }
}