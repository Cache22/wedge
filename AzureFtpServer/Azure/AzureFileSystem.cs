﻿using System.IO;
using System.Linq;
using System.Collections.Generic;
using AzureFtpServer.Ftp.FileSystem;
using AzureFtpServer.Provider;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AzureFtpServer.Azure
{
    public class AzureFileSystem : IFileSystem
    {
        private readonly AzureBlobStorageProvider _provider;
        private string _containerName;

        // Constructor
        public AzureFileSystem(string storageAccount, string containerName, bool sendQueueNotificationsOnUpload)
        {
            // Set container name (if none specified, specify the development container default)
            _containerName = !string.IsNullOrEmpty(containerName) ? containerName : "DevelopmentContainer";
            _provider = new AzureBlobStorageProvider(storageAccount, _containerName, sendQueueNotificationsOnUpload);
        }

        #region Implementation of IFileSystem

        public IFile OpenFile(string sPath, bool fWrite)
        {
            var f = new AzureFile();
            if (fWrite == true)
            {
                f.BlobStream = _provider.GetWriteBlobStream(sPath);
            }
            else
            {
                f.BlobStream = _provider.GetReadBlobStream(sPath);
            }

            if (f.BlobStream == null)
                return null;

            return f;
        }

        public IFileInfo GetFileInfo(string sPath)
        {
            return new AzureFileInfo(_provider.GetBlobInfo(sPath, false));
        }

        public IFileInfo GetDirectoryInfo(string sDirPath)
        {
            return new AzureFileInfo(_provider.GetBlobInfo(sDirPath, true));
        }

        /// <summary>
        /// Get the filename list in the directory 
        /// </summary>
        /// <param name="sDirPath">directory path</param>
        /// <returns>an arry of filenames</returns>
        public string[] GetFiles(string sDirPath)
        {
            IEnumerable<ICloudBlob> files = _provider.GetFileListing(sDirPath);
            
            string[] result = files
                .Select(r => r.Uri.AbsolutePath)
                .Select(System.Uri.UnescapeDataString)
                .Where(_ => !_.Contains(AzureBlobStorageProvider.CONTAINER_PLACEHOLDER_FILENAME))
                .ToArray()
                .ToFtpPath(sDirPath);

            return result;
        }

        /// <summary>
        /// Get the directory name list in the directory 
        /// </summary>
        /// <param name="sDirPath">directory path</param>
        /// <returns>an arry of directorynames</returns>
        public string[] GetDirectories(string sDirPath)
        {
            IEnumerable<CloudBlobDirectory> directories = _provider.GetDirectoryListing(sDirPath);
            
            string[] result =  directories
                .Select(r => r.Uri.AbsolutePath)
                .Select(System.Uri.UnescapeDataString)
                .ToArray()
                .ToFtpPath(sDirPath);

            return result;
        }

        /// <summary>
        /// check if the directory exists
        /// </summary>
        /// <param name="sPath">the directory name, final char is '/'</param>
        /// <returns></returns>
        public bool DirectoryExists(string sDirPath)
        {
            return _provider.IsValidDirectory(sDirPath);
        }

        /// <summary>
        /// check if the file exists
        /// </summary>
        /// <param name="sPath">the file name</param>
        /// <returns></returns>
        public bool FileExists(string sPath)
        {
            return _provider.Exists(sPath);
        }

        public bool CreateDirectory(string sPath)
        {
            return _provider.CreateDirectory(sPath);
        }

        public bool Move(string sOldPath, string sNewPath)
        {
            return _provider.Rename(sOldPath, sNewPath) == StorageOperationResult.Completed;
        }

        public bool DeleteFile(string sPath)
        {
            return _provider.DeleteFile(sPath);
        }

        public bool DeleteDirectory(string sPath)
        {
            return _provider.DeleteDirectory(sPath);
        }

        public bool AppendFile(string sPath, Stream stream)
        {
            return _provider.AppendFileFromStream(sPath, stream);
        }

        public void Log4Upload(string sPath)
        {
            _provider.UploadNotification(sPath);
        }

        public void SetFileMd5(string sPath, string md5Value)
        {
            _provider.SetBlobMd5(sPath, md5Value);
        }

        #endregion
    }
}
