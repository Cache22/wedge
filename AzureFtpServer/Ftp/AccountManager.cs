using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using AzureFtpServer.Provider;

namespace AzureFtpServer.Ftp
{
    /// <summary>
    /// AccountManager Class
    /// Read account information from config settings and store valid (username,password) pairs
    /// </summary>
    public class AccountManager
    {
        public readonly Func<string, string, bool> CheckAccount;

        #region Construction

        public AccountManager(Func<string, string, bool> checkAccount)
        {
            if (checkAccount == null) throw new ArgumentNullException("checkAccount");

            this.CheckAccount = checkAccount;
        }

        public AccountManager(Dictionary<string, string> accounts)
        {
            this.CheckAccount = (u, p) => accounts.ContainsKey(u) && accounts[u] == p;
        }

        public AccountManager(string accountInfo)
        {
            Dictionary<string, string> accounts =  AccountManager.ParseOldConfiguration(accountInfo);

            this.CheckAccount = (u, p) => accounts.ContainsKey(u) && accounts[u] == p;
        }

        #endregion

        public static Dictionary<string, string> ParseOldConfiguration(string accountInfo)
        {
            const char separator = ':';

            var _accounts = new Dictionary<string, string>();

            while (true)
            {
                // Get the begin tag (
                int beginIdx = accountInfo.IndexOf('(');
                // Get the end tag )
                int endIdx = accountInfo.IndexOf(')');

                if ((beginIdx < 0) || (endIdx < 0) || (beginIdx > endIdx) || (endIdx == beginIdx + 1))
                    break;

                string oneAccount = accountInfo.Substring(beginIdx + 1, endIdx - beginIdx - 1);

                // modify accountInfo for loop
                accountInfo = accountInfo.Substring(endIdx + 1);

                int separatoridx = oneAccount.IndexOf(separator);

                if (separatoridx < 0)
                {
                    Trace.WriteLine(string.Format("Invalid <username, password> pair ({0}) in cscfg.", oneAccount), "Warnning");
                    continue;
                }

                // get the username substr
                string username = oneAccount.Substring(0, separatoridx);

                // check the username whether conform to the naming rules
                if (!CheckUsername(username))
                {
                    Trace.WriteLine(string.Format("Invalid <username, password> pair ({0}) in cscfg.", oneAccount), "Warnning");
                    continue;
                }

                // check whether the username already exists
                if (_accounts.ContainsKey(username))
                    continue;

                // get the password substr
                string password = oneAccount.Substring(separatoridx + 1);
                // simple check, password can not be empty
                if (password.Length == 0)
                    continue;

                _accounts.Add(username, password);
            }

            Trace.WriteLine(string.Format("Load {0} accounts.", _accounts.Count), "Information");

            return _accounts;
        }

        /// <summary>
        /// checks whether username conform to the Azure container naming rules
        /// 1. start with a letter or number, and can contain only letters, numbers, and the dash (-) character
        /// 2. Every dash (-) character must be immediately preceded and followed by a letter or number; 
        ///    consecutive dashes are not permitted in container names.
        /// 3. All letters in a container name must be lowercase.
        /// 4. Container names must be from 3 through 63 characters long.
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        private static bool CheckUsername(string username)
        {
            return Regex.IsMatch(username, 
                @"^\$root$|^[a-z0-9]([a-z0-9]|(?<=[a-z0-9])-(?=[a-z0-9])){2,62}$");
        }
    }
}
