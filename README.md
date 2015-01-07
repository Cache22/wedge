

Taken from https://ftp2azure.codeplex.com/, adapted to new Storage SDK



FTP to Azure Blob Storage Bridge
################################


# Project Description

Deployed in a worker role, the code creates an FTP server that can accept connections from all popular FTP clients (like FileZilla, for example) for command and control of your blob storage account.

# What does it do?

It was created to enable each authenticated user to upload to their own unique container within your blob storage account, and restrict them to that container only (exactly like their 'home directory'). You can, however, modify the authentication code very simply to support almost any configuration you can think of.

Using this code, it is possible to actually configure an FTP server much like any traditional deployment and use blob storage for multiple users. 

This project contains slightly modified source code from  [Mohammed Habeeb's project, "C# FTP Server"](http://www.codeguru.com/csharp/csharp/cs_internet/desktopapplications/article.php/c13163) and runs in an Azure Worker Role.

Can I help out?
Most certainly. If you're interested, please download the source code, start tinkering and get in touch!

Authors: https://www.codeplex.com/site/users/view/richardparker


