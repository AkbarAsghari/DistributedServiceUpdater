using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DistributedServiceUpdater.Utilities
{

    public class GenericRestSharp<T>
    {
        private RestClient client;
        private RestRequest request;

        public HttpStatusCode StatusCode { get; private set; }
        public string Url { get; private set; }
        public string ResponseContent { get; private set; }
        public bool HaveException { get; private set; }

        public GenericRestSharp()
        {
            client = new RestClient();
            client.Timeout = -1;

            request = new RestRequest();
        }
        public GenericRestSharp<T> AddHeader(string value, string header = "Authorization")
        {
            request.AddHeader(header, value);
            return this;
        }

        public GenericRestSharp<T> SetUrl(string url)
        {
            Url = url;
            client.BaseUrl = new Uri(url);
            return this;
        }

        public GenericRestSharp<T> SetMethod(Method method)
        {
            request.Method = method;
            return this;
        }

        public GenericRestSharp<T> SetBody(object body, string type = "application/json")
        {
            request.AddParameter(type, JsonConvert.SerializeObject(body), ParameterType.RequestBody);
            return this;
        }

        public GenericRestSharp<T> SetQueryParameters(Dictionary<string, object> queryParameters)
        {
            client.BaseUrl = new Uri(client.BaseUrl?.ToString()
                + "?"
                + String.Join("&", queryParameters.Select(x => x.Key.ToString() + "=" + x.Value.ToString())));
            return this;
        }

        public GenericRestSharp<T> SetParameters(Dictionary<string, object> queryParameters)
        {
            foreach (var parameter in queryParameters)
                request.AddParameter(parameter.Key, parameter.Value);
            return this;
        }



        public T Execute()
        {
            if (String.IsNullOrEmpty(Url))
                throw new ArgumentNullException(nameof(Url));

            IRestResponse response = client.Execute(request);

            StatusCode = response.StatusCode;
            ResponseContent = response.Content;

            T responseObj = default(T);

            try
            {
                responseObj = JsonConvert.DeserializeObject<T>(response.Content);
            }
            catch { HaveException = true; }

            return responseObj;
        }
    }
}
