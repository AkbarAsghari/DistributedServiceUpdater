using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DistributedServiceUpdater
{
    public class BasicAuthentication : IAuthentication
    {

        private string Username { get; }
        private string Password { get; }

        /// <summary>
        ///     Initializes credentials for Basic Authentication.
        /// </summary>
        /// <param name="username">Username to use for Basic Authentication</param>
        /// <param name="password">Password to use for Basic Authentication</param>
        public BasicAuthentication(string username, string password)
        {
            Username = username;
            Password = password;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var token = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{Username}:{Password}"));
            return $"Basic {token}";
        }

        public void Apply(ref WebClient webClient)
        {
            webClient.Headers[HttpRequestHeader.Authorization] = ToString();
        }
    }
}
