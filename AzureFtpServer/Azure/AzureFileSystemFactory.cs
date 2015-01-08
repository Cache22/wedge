using AzureFtpServer.Ftp.FileSystem;
using AzureFtpServer.Ftp;

namespace AzureFtpServer.Azure
{
    public class AzureFileSystemFactory : IFileSystemClassFactory
    {
        #region Member variables
        private AccountManager m_accountManager;
        private string m_storageAccount;
        #endregion

        #region Construction
        
        public AzureFileSystemFactory(string storageAccount)
        {
            this.m_storageAccount = storageAccount;
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
            var system = new AzureFileSystem(m_storageAccount, containerName);
            
            return system;
        }

        #endregion

    }
}