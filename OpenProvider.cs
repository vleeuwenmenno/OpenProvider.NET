using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nager.PublicSuffix;
using Newtonsoft.Json;
using RestSharp;

namespace OpenProvider.NET
{
    public class OP
    {
        private string authToken {get; set;}
        private string baseUrl {get; set;}
        private string baseVersion {get; set;}
        public long resellerId {get; set;}
        public bool useDomainCache {get;set;}

        public string domainsCache
        {
            get
            {
                if (File.Exists(Environment.CurrentDirectory + "/domains.cache"))
                {
                    DateTime cache = File.GetLastWriteTime(Environment.CurrentDirectory + "/domains.cache");

                    if ((DateTime.Now - cache).Hours > 24)
                    {
                        File.Delete(Environment.CurrentDirectory + "/domains.cache");
                        return null;
                    }
                    else
                        return File.ReadAllText(Environment.CurrentDirectory + "/domains.cache");
                }
                
                else
                    return null;
            }
            set
            {
                File.WriteAllText(Environment.CurrentDirectory + "/domains.cache", value);
            }
        }

        private bool request(string url, Method method, out string result, Dictionary<string, dynamic> requestBody = null)
        {
            var client = new RestClient($"{baseUrl}{baseVersion}{url}");
            var request = new RestRequest(method);
            request.AddHeader("Authorization", $"Bearer {this.authToken}");

            if (requestBody != null)
            { 
                request.AddHeader("Content-Type", "application/json");
                request.AddParameter("application/json", JsonConvert.SerializeObject(requestBody), ParameterType.RequestBody);
            }

            IRestResponse response = client.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                result = response.Content;
                return true;
            }
            else
            {
                result = null;
                return false;;
            }
        }

        public Dictionary<string, Dictionary<string, string>> QueryDomain(string[] domains)
        {
            List<Dictionary<string, string>> domainList = new List<Dictionary<string, string>>();

            foreach(string domain in domains)
            {
                Nager.PublicSuffix.DomainInfo info = new DomainParser(new WebTldRuleProvider()).Parse(domain);
                domainList.Add(new Dictionary<string, string>() {
                    { "name", info.Domain },
                    { "extension", info.TLD }
                });
            }

            bool success = request("/domains/check", Method.POST, out string resp, new Dictionary<string, dynamic> () 
            {
                { "application_mode", "preregistration" },
                { "domains", domainList },
                { "with_price", true }
            });

            if (success)
            {
                Dictionary<string, Dictionary<string, string>> returnDomains = new Dictionary<string, Dictionary<string, string>>();
                foreach (Dictionary<string, object> obj in ((Newtonsoft.Json.Linq.JArray)((Newtonsoft.Json.Linq.JObject)((Newtonsoft.Json.Linq.JObject)JsonConvert.DeserializeObject(resp)).ToObject<Dictionary<string, object>>()["data"]).ToObject<Dictionary<string, object>>()["results"]).ToObject<List<Dictionary<string, object>>>())
                {
                    Dictionary<string, string> domain = new Dictionary<string, string>();

                    domain.Add("status", (string)obj["status"]);

                    if (obj.ContainsKey("reason"))
                        domain.Add("reason", (string)obj["reason"]);               

                    if (!domain.ContainsKey("reason") || domain.ContainsKey("reason") && domain["reason"] != "In use")
                    {
                        domain.Add("price", (((Newtonsoft.Json.Linq.JObject)obj["price"])["product"]).ToObject<Dictionary<string, object>>()["price"].ToString());
                        domain.Add("currency", (((Newtonsoft.Json.Linq.JObject)obj["price"])["product"]).ToObject<Dictionary<string, object>>()["currency"].ToString());
                    }

                    returnDomains.Add((string)obj["domain"], domain);
                }

                return returnDomains;
            }
            else
                return null;
        }
        /*
        {
            "application_mode": "preregistration",
            "domains": [
                {
                "extension": "me",
                "name": "vleeuwen"
                }
            ],
            "with_price": true
        }
*/

        public bool Authenticate(string user, string pass)
        {
            return Authenticate(user, pass, out Dictionary<string, object> notUsed);
        }

        public bool Authenticate(string user, string pass, out Dictionary<string, object> ifconfigResult)
        {
            var client = new RestClient($"{baseUrl}{baseVersion}/auth/login");
            var request = new RestRequest(Method.POST);
            ifconfigResult = Utilities.ifconfig;
            Dictionary<string, string> body = new Dictionary<string, string>() 
            {
                { "ip", (string)ifconfigResult["ip"] },
                { "username", user },
                { "password", pass }
            };
            
            request.AddHeader("Content-Type", "application/json");
            request.AddParameter("application/json", JsonConvert.SerializeObject(body), ParameterType.RequestBody);

            IRestResponse response = client.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                Dictionary<string, object> resp = JsonConvert.DeserializeObject<Dictionary<string, object>>(response.Content);
                Dictionary<string, string> data = ((Newtonsoft.Json.Linq.JObject)resp["data"]).ToObject<Dictionary<string, string>>();

                authToken = data["token"];
                resellerId = long.Parse(data["reseller_id"]);

                return true;
            }
            else
                return false;
        }

        public List<DomainInfo> GetAllDomains(int limit = 1000, int offset = 0)
        {
            // Check if our cache is available
            bool resultStatus = false;
            string result = "";

            if (this.domainsCache == null)
                resultStatus = request("/domains?limit="+limit+"&offset="+offset, Method.GET, out result);         
            else
            {
                result = this.domainsCache;
                resultStatus = true;
            }

            object parsed = JsonConvert.DeserializeObject(result);

            if (resultStatus)
            {
                List<Dictionary<string, object>> resp = ((Newtonsoft.Json.Linq.JObject)parsed)["data"]["results"].ToObject<List<Dictionary<string, object>>>();
                List<DomainInfo> domains = resp.Select(x => new DomainInfo(x)).ToList();
                
                this.domainsCache = result;
                return domains;
            }
            else
                return null;
        }

        public void InvalidateDomainCache()
        {
            if (File.Exists(Environment.CurrentDirectory + "/domains.cache"))
                File.Delete(Environment.CurrentDirectory + "/domains.cache");
        }

        public OP(string baseUrl = "https://api.openprovider.eu/", string baseVersion = "v1beta")
        {
            this.baseUrl = baseUrl;
            this.baseVersion = baseVersion;
            this.useDomainCache = true;
        }

        public OP(bool useDomainCache)
        {
            this.baseUrl = "https://api.openprovider.eu/";
            this.baseVersion = "v1beta";
            this.useDomainCache = useDomainCache;
        }

        public OP()
        {
            this.baseUrl = "https://api.openprovider.eu/";
            this.baseVersion = "v1beta";
            this.useDomainCache = true;
        }
    }
}