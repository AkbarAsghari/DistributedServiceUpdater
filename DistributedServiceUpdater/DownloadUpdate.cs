using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Mime;
using System.Security.Cryptography;
using System.ServiceProcess;
using System.Text;

namespace DistributedServiceUpdater
{
    public class DownloadUpdate
    {
        private readonly UpdateModel _args;

        public DownloadUpdate(UpdateModel args)
        {
            _args = args;
        }

        private string _tempFile;

        private string _tempBackupPath;

        private string _servicePath;

        private WebClient _webClient;

        public void Start()
        {
            var uri = new Uri(_args.DownloadURL);

            _webClient = ServiceUpdater.GetWebClient(uri, ServiceUpdater.BasicAuthDownload);

            if (string.IsNullOrEmpty(ServiceUpdater.DownloadPath))
            {
                _tempFile = Path.GetTempFileName();
            }
            else
            {
                _tempFile = Path.Combine(ServiceUpdater.DownloadPath, $"{Guid.NewGuid().ToString()}.tmp");
                if (!Directory.Exists(ServiceUpdater.DownloadPath))
                {
                    Directory.CreateDirectory(ServiceUpdater.DownloadPath);
                }
            }

            _webClient.DownloadFileCompleted += WebClientOnDownloadFileCompleted;

            _webClient.DownloadFileAsync(uri, _tempFile);
        }

        private void WebClientOnDownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                if (_args.CheckSum != null)
                {
                    //CompareChecksum(_tempFile, _args.CheckSum);
                }


                if (!IsServiceExist())
                {
                    if (String.IsNullOrEmpty(ServiceUpdater.ServiceInstallPath))
                    {
                        _servicePath = Path.Combine(@"C:\Program Files (x86)", _args.ServiceName);
                    }
                    else
                    {
                        _servicePath = Path.Combine(ServiceUpdater.ServiceInstallPath, _args.ServiceName);
                    }
                }
                //else _servicePath is entry
                ChangeServiceStatus(false);
                KillServiceProcess();
                CreateTempBackupPath();
                MoveFile(_servicePath, _tempBackupPath);
                ClearFilesAttributes(_servicePath);
                DeleteDirectory(_servicePath);
                Directory.CreateDirectory(_servicePath);
                ZipFile.ExtractToDirectory(_tempFile, _servicePath);

                //if service was installed before it need just start
                if (!IsServiceExist())
                    InstallNewService();

                ChangeServiceStatus(true);
            }
            catch (Exception ex)
            {
                ClearFilesAttributes(_servicePath);
                DeleteDirectory(_servicePath);
                MoveFile(_tempBackupPath, _servicePath);
            }
        }

        private void InstallNewService()
        {
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                }
            };
            process.Start();
            process.StandardInput.WriteLine($"SC CREATE { _args.ServiceName } binpath=\"" + Path.Combine(_servicePath, _args.ServiceName + ".exe") + "\"");
            process.StandardInput.Flush();
            process.StandardInput.Close();
            process.WaitForExit();
        }

        private void CreateTempBackupPath()
        {
            var tempPath = Path.GetTempPath();

            var backupDir = Path.Combine(tempPath, "backup", _args.ServiceName);

            if (Directory.Exists(backupDir))
                DeleteDirectory(backupDir);

            Directory.CreateDirectory(backupDir);

            _tempBackupPath = backupDir;
        }

        private void DeleteDirectory(string path)
        {
            if (Directory.Exists(path) || File.Exists(path))
            {
                foreach (string directory in Directory.GetDirectories(path))
                {
                    DeleteDirectory(directory);
                }

                try
                {
                    foreach (var filePath in Directory.GetFiles(path))
                    {
                        File.SetAttributes(filePath, FileAttributes.Normal);
                        File.Delete(filePath);
                    }
                    Directory.Delete(path, true);
                }
                catch (IOException)
                {
                    DeleteDirectory(path);
                }
            }

        }

        private void ClearFilesAttributes(string currentDir)
        {
            if (Directory.Exists(currentDir))
            {
                FileInfo fileInfoDir = new FileInfo(currentDir);
                fileInfoDir.IsReadOnly = false;

                File.SetAttributes(currentDir, FileAttributes.Normal);

                string[] subDirs = Directory.GetDirectories(currentDir);
                foreach (string dir in subDirs)
                {
                    ClearFilesAttributes(dir);
                }

                string[] files = Directory.GetFiles(currentDir);
                foreach (string file in files)
                {
                    FileInfo fileInfo = new FileInfo(file);
                    fileInfo.IsReadOnly = false;
                    File.SetAttributes(file, FileAttributes.Normal);
                }
            }
        }

        public static void MoveFile(string sourcePath, string destinationPath)
        {
            //Create directories
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(sourcePath, destinationPath));

            //Copy files
            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                if (File.Exists(newPath.Replace(sourcePath, destinationPath)))
                {
                    byte[] newFileBytes = File.ReadAllBytes(newPath.Replace(sourcePath, destinationPath));

                    using (var stream = new System.IO.FileStream(newPath, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite))
                    {
                        stream.Write(newFileBytes, 0, newFileBytes.Length);
                        stream.Close();
                    }
                }
                else
                    File.Copy(newPath, newPath.Replace(sourcePath, destinationPath), true);

            }
        }

        private void KillServiceProcess()
        {
            Process[] procs = Process.GetProcessesByName(_args.ServiceName);
            if (procs.Length > 0)
            {
                foreach (Process proc in procs)
                {
                    //do other stuff if you need to find out if this is the correct proc instance if you have more than one
                    proc.Close();
                    proc.Kill();
                }
            }
        }

        /// <summary>
        /// if false : service stop else start
        /// </summary>
        /// <param name="start"></param>
        void ChangeServiceStatus(bool start)
        {
            ServiceController service = new ServiceController(_args.ServiceName);

            if ((service.Status.Equals(ServiceControllerStatus.Stopped) ||
           service.Status.Equals(ServiceControllerStatus.StopPending)) && start)
            {
                service.Start();
            }
            else if (!start)
            {
                service.Stop();
            }
        }


        private bool IsServiceExist()
        {
            RegistryKey regkey = Registry.LocalMachine.OpenSubKey(string.Format(@"SYSTEM\CurrentControlSet\services\{0}", _args.ServiceName));

            if (regkey != null)
                if (regkey.GetValue("ImagePath") != null)
                {
                    string path = regkey.GetValue("ImagePath").ToString().Replace("\"", string.Empty);
                    _servicePath = Path.GetDirectoryName(path);
                    regkey.Dispose();
                    return true;
                }
            return false;
        }

    }
}
