using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Threading.Tasks;

namespace DistributedServiceUpdater
{
    public class ServiceUpdater
    {
        public static string AppCastURL;

        /// <summary>
        ///     if dont set provide Temp Path
        /// </summary>
        public static string DownloadPath;

        public static string ServiceInstallPath;

        internal static Uri BaseUri;
        /// <summary>
        /// for download File
        /// </summary>
        public static IAuthentication BasicAuthDownload;

        /// <summary>
        /// for download Json
        /// </summary>
        public static IAuthentication BasicAuthXML;

        public static NetworkCredential FtpCredentials;



        public static object CheckVersion()
        {
            UpdateModel[] versionModels = DownloadVersion();
            if (versionModels.Length == 0)
                return null;

            foreach (var versionModel in versionModels)
            {
                if (versionModel.Version == CurrentServiceVersion(versionModel.ServiceName))
                    continue;

                new DownloadUpdate(versionModel).Start();

                AddOrUpdateAppsVersion(versionModel.ServiceName, versionModel.Version);
            }

            return null;
        }

        internal static string CurrentServiceVersion(string serviceName)
        {
            var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var settings = configFile.AppSettings.Settings;
            var version = settings[serviceName];
            if (version == null)
                return "0.0.0";
            else
                return version.Value;
        }

        internal static void AddOrUpdateAppsVersion(string key, string value)
        {
            var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var settings = configFile.AppSettings.Settings;
            if (settings[key] == null)
            {
                settings.Add(key, value);
            }
            else
            {
                settings[key].Value = value;
            }
            configFile.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
        }
        internal static UpdateModel[] DownloadVersion()
        {
            BaseUri = new Uri(AppCastURL);

            using (WebClient client = GetWebClient(BaseUri, BasicAuthXML))
            {
                string json = client.DownloadString(BaseUri);

                return JsonConvert.DeserializeObject<UpdateModel[]>(json);
            }
        }

        internal static WebClient GetWebClient(Uri uri, IAuthentication basicAuthentication)
        {
            WebClient webClient = new WebClient
            {
                CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore)
            };

            if (uri.Scheme.Equals(Uri.UriSchemeFtp))
            {
                webClient.Credentials = FtpCredentials;
            }
            else
            {
                basicAuthentication?.Apply(ref webClient);
            }

            return webClient;
        }

    }
}
