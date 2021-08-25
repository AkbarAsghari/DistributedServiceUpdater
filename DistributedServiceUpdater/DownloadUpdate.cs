using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Mime;
using System.Security.Cryptography;
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
                    CompareChecksum(_tempFile, _args.CheckSum);
                }

                ContentDisposition contentDisposition = null;
                if (!String.IsNullOrWhiteSpace(_webClient.ResponseHeaders?["Content-Disposition"]))
                {
                    contentDisposition = new ContentDisposition(_webClient.ResponseHeaders["Content-Disposition"]);
                }

                var fileName = string.IsNullOrEmpty(contentDisposition?.FileName)
                    ? Path.GetFileName(_webClient.ResponseUri.LocalPath)
                    : contentDisposition.FileName;

                var tempPath =
                    Path.Combine(
                        string.IsNullOrEmpty(ServiceUpdater.DownloadPath) ? Path.GetTempPath() : ServiceUpdater.DownloadPath,
                        fileName);

                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }

                File.Move(_tempFile, tempPath);

                string installerArgs = null;
                if (!string.IsNullOrEmpty(_args.InstallerArgs))
                {
                    installerArgs = _args.InstallerArgs.Replace("%path%",
                        Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName));
                }

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = tempPath,
                    UseShellExecute = true,
                    Arguments = installerArgs ?? string.Empty
                };

                var extension = Path.GetExtension(tempPath);
                if (extension.Equals(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    string installerPath = Path.Combine(Path.GetDirectoryName(tempPath) ?? throw new InvalidOperationException(), "ZipExtractor.exe");

                    File.WriteAllBytes(installerPath, Resources.ZipExtractor);

                    string executablePath = Process.GetCurrentProcess().MainModule?.FileName;
                    string extractionPath = Path.GetDirectoryName(executablePath);

                    if (!string.IsNullOrEmpty(AutoUpdater.InstallationPath) &&
                        Directory.Exists(AutoUpdater.InstallationPath))
                    {
                        extractionPath = AutoUpdater.InstallationPath;
                    }

                    StringBuilder arguments =
                        new StringBuilder($"\"{tempPath}\" \"{extractionPath}\" \"{executablePath}\"");
                    string[] args = Environment.GetCommandLineArgs();
                    for (int i = 1; i < args.Length; i++)
                    {
                        if (i.Equals(1))
                        {
                            arguments.Append(" \"");
                        }

                        arguments.Append(args[i]);
                        arguments.Append(i.Equals(args.Length - 1) ? "\"" : " ");
                    }

                    processStartInfo = new ProcessStartInfo
                    {
                        FileName = installerPath,
                        UseShellExecute = true,
                        Arguments = arguments.ToString()
                    };
                }
                else if (extension.Equals(".msi", StringComparison.OrdinalIgnoreCase))
                {
                    processStartInfo = new ProcessStartInfo
                    {
                        FileName = "msiexec",
                        Arguments = $"/i \"{tempPath}\"",
                    };
                    if (!string.IsNullOrEmpty(installerArgs))
                    {
                        processStartInfo.Arguments += " " + installerArgs;
                    }
                }

                if (AutoUpdater.RunUpdateAsAdmin)
                {
                    processStartInfo.Verb = "runas";
                }

                try
                {
                    Process.Start(processStartInfo);
                }
                catch (Win32Exception exception)
                {
                    if (exception.NativeErrorCode == 1223)
                    {
                        _webClient = null;
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, e.GetType().ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
                _webClient = null;
            }
            finally
            {
                DialogResult = _webClient == null ? DialogResult.Cancel : DialogResult.OK;
                FormClosing -= DownloadUpdateDialog_FormClosing;
                Close();
            }
        }

        private void CompareChecksum(string tempFile, string checkSum)
        {
            throw new NotImplementedException();
        }
    }
}
