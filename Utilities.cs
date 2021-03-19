using System;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;

namespace OpenProvider.NET
{
    public class Utilities
    {
        public static Dictionary<string, object> ifconfig
        {
            get 
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri("http://ifconfig.co/"),
                    Headers =
                    {
                        { "Accept", "application/json" },
                    }
                };

                string body = "";
                using (var response = client.Send(request))
                {
                    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        client = new HttpClient();
                        request = new HttpRequestMessage
                        {
                            Method = HttpMethod.Get,
                            RequestUri = new Uri("http://ifconfig.me/"),
                            Headers =
                            {
                                { "Accept", "application/json" },
                            }
                        };

                        body = "";
                        using (var responseRetry = client.Send(request))
                        {
                            responseRetry.EnsureSuccessStatusCode();
                            body = responseRetry.Content.ReadAsStringAsync().Result;
                            body = "{ \"ip\": \""+body+"\" }";
                        }
                    }
                    else
                        body = response.Content.ReadAsStringAsync().Result;

                }
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(body);
            }
        }
    }
}