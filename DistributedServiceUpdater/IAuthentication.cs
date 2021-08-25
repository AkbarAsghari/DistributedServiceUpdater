using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DistributedServiceUpdater
{
    public interface IAuthentication
    {
        /// <summary>
        ///     Apply the authentication to webclient.
        /// </summary>
        /// <param name="webClient">WebClient for which you want to use this authentication method.</param>
        void Apply(ref WebClient webClient);
    }
}
