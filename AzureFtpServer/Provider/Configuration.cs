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
    }
}