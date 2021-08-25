using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DistributedServiceUpdater
{
    public class CustomAuthentication : IAuthentication
    {
        private string HttpRequestHeaderAuthorizationValue { get; }

        /// <summary>
        ///     Initializes authorization header value for Custom Authentication
        /// </summary>
        /// <param name="httpRequestHeaderAuthorizationValue">Value to use as http request header authorization value</param>
        public CustomAuthentication(string httpRequestHeaderAuthorizationValue)
        {
            HttpRequestHeaderAuthorizationValue = httpRequestHeaderAuthorizationValue;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return HttpRequestHeaderAuthorizationValue;
        }

        /// <inheritdoc />
        public void Apply(ref WebClient webClient)
        {
            webClient.Headers[HttpRequestHeader.Authorization] = ToString();
        }
    }
}
