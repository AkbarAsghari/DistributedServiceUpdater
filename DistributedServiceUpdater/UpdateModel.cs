using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedServiceUpdater
{
    public class UpdateModel
    {
        public string ServiceName { get; set; }
        public string ServiceExeFileName { get; set; }
        public string Version { get; set; }
        public string CheckSum { get; set; }
        public string DownloadURL { get; set; }
    }
}
