using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedServiceUpdater
{
    public class VersionModel
    {
        public string ServiceName { get; set; }
        public string Version { get; set; }
        public string Hash { get; set; }
        public string Url { get; set; }
    }
}
