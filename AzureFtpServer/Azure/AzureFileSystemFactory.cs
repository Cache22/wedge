using AzureFtpServer.Ftp.FileSystem;
using AzureFtpServer.Ftp;

namespace AzureFtpServer.Azure
{
    public class AzureFileSystemFactory : IFileSystemClassFactory
    {
        #region Member variables
        private AccountManager m_accountManager;
        private string m_storageAccount;
        private bool m_sendQueueNotificationsOnUpload;
        #endregion

        #region Construction
        
        public AzureFileSystemFactory(string storageAccount, bool sendQueueNotificationsOnUpload)
        {
            this.m_storageAccount = storageAccount;
            this.m_sendQueueNotificationsOnUpload = sendQueueNotificationsOnUpload;
            m_accountManager = new AccountManager();
            m_accountManager.LoadConfigration();
        }
        #endregion

        #region Implementation of IFileSystemClassFactory

        public IFileSystem Create(string sUser, string sPassword)
        {
            if ((sUser == null) || (sPassword == null))
                return null;

            if (!m_accountManager.CheckAccount(sUser, sPassword))
                return null;
            
            string containerName = sUser;
            var system = new AzureFileSystem(
                storageAccount: m_storageAccount, 
                containerName: containerName, 
                sendQueueNotificationsOnUpload: m_sendQueueNotificationsOnUpload);
            
            return system;
        }

        #endregion

    }
}