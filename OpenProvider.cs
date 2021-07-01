using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        public int domainCacheExpireHours {get;set;}

        public string domainsCache
        {
            get
            {
                if (File.Exists(Environment.CurrentDirectory + "/domains.cache"))
                {
                    DateTime cache = File.GetLastWriteTime(Environment.CurrentDirectory + "/domains.cache");

                    if ((DateTime.Now - cache).TotalHours > domainCacheExpireHours)
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
                result = response.Content;
                return false;;
            }
        }

        public Dictionary<string, Dictionary<string, string>> QueryDomain(string[] domains)
        {
            List<Dictionary<string, string>> domainList = new List<Dictionary<string, string>>();

            foreach(string domain in domains)
            {
                domainList.Add(new Dictionary<string, string>() {
                    { "name", domain },
                    { "extension", domain.Split('.').Last() }
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

        public bool Authenticate(string user, string pass, bool dev)
        {
            return Authenticate(user, pass, dev, out Dictionary<string, object> notUsed);
        }

        public bool Authenticate(string user, string pass, bool dev, out Dictionary<string, object> ifconfigResult)
        {
            var client = new RestClient($"{baseUrl}{baseVersion}/auth/login");
            var request = new RestRequest(Method.POST);

            if (dev)
                ifconfigResult = new Dictionary<string, object>()
                {
                    {"ip", ""}
                };
            else
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

        [Obsolete("This function uses the XML API as the REST API is currently broken for this function!")]
        public List<NameServerGroup> GetNSGroups(string user, string pass)
        {
            var client = new RestClient($"{this.baseUrl}/searchNsGroupRequest");
            var request = new RestRequest(Method.POST);

            request.AddHeader("Content-Type", "application/xml");
            request.AddParameter("undefined", $@"<?xml version=""1.0"" encoding=""UTF-8""?>
                                                <openXML>
                                                    <credentials>
                                                        <username>{user}</username>
                                                        <password>{pass}</password>
                                                    </credentials>
                                                    <searchNsGroupRequest />
                                                </openXML>", ParameterType.RequestBody);
            
            IRestResponse response = client.Execute(request);
            XmlDocument responseNode = new XmlDocument();
            Dictionary<string, object> responseDocument = new Dictionary<string, object>();

            responseNode.LoadXml(response.Content);

            List<NameServerGroup> groups = new List<NameServerGroup>();
            XmlNodeList xnList = responseNode.SelectNodes("/openXML/reply/data/results/array/item");
            foreach (XmlNode xn in xnList)
            {
                NameServerGroup group = new NameServerGroup();

                group.name = xn.SelectSingleNode("nsGroup").InnerText;
                group.id = long.Parse(xn.SelectSingleNode("id").InnerText);

                foreach (XmlNode nsObj in xn.SelectNodes("nameServers/array/item"))
                {
                    NameServer ns = new NameServer();

                    ns.id = long.Parse(nsObj.SelectSingleNode("id").InnerText);
                    ns.seqNo = long.Parse(nsObj.SelectSingleNode("seqNr").InnerText);
                    ns.domainName = nsObj.SelectSingleNode("name").InnerText;
                    ns.ipv4 = nsObj.SelectSingleNode("ip").InnerText;
                    ns.ipv6 = nsObj.SelectSingleNode("ip6").InnerText;

                    group.nameServers.Add(ns);
                }
                groups.Add(group);
            }
            return groups;
        }

        public bool CreateNSGroup(List<NameServer> nameServers, string groupName)
        {
            if (string.IsNullOrEmpty(groupName))
                return false;
            
            List<Dictionary<string, object>> nameServerDicts = new List<Dictionary<string, object>>();
            foreach (NameServer ns in nameServers)
            {
                Dictionary<string, object> nsDict = new Dictionary<string, object>();

                nsDict.Add("ip", ns.ipv4);
                nsDict.Add("ip6", ns.ipv6);
                nsDict.Add("name", ns.domainName);
                nsDict.Add("seq_nr", ns.seqNo);
                
                nameServerDicts.Add(nsDict);
            }

            bool success = request("/dns/nameservers/groups", Method.POST, out string resp, new Dictionary<string, dynamic> () 
            {
                { "name_servers", nameServerDicts },
                { "ns_group", groupName }
            });

            if (!success)
                return false;

            Dictionary<string, object> responseJson = JsonConvert.DeserializeObject<Dictionary<string, object>>(resp);

            if (((JObject)responseJson["data"]).ToObject<Dictionary<string, bool>>()["success"] == true)
                return true;
            else
                return false;
        }

        public bool UpdateNSGroup(List<NameServer> nameServers, string groupName)
        {
            if (string.IsNullOrEmpty(groupName))
                return false;
            
            List<Dictionary<string, object>> nameServerDicts = new List<Dictionary<string, object>>();
            foreach (NameServer ns in nameServers)
            {
                Dictionary<string, object> nsDict = new Dictionary<string, object>();

                nsDict.Add("ip", ns.ipv4);
                nsDict.Add("ip6", ns.ipv6);
                nsDict.Add("name", ns.domainName);
                nsDict.Add("seq_nr", ns.seqNo);
                
                nameServerDicts.Add(nsDict);
            }

            bool success = request("/dns/nameservers/groups/"+groupName, Method.PUT, out string resp, new Dictionary<string, dynamic> () 
            {
                { "name_servers", nameServerDicts }
            });

            if (!success)
                return false;

            Dictionary<string, object> responseJson = JsonConvert.DeserializeObject<Dictionary<string, object>>(resp);

            if (((JObject)responseJson["data"]).ToObject<Dictionary<string, bool>>()["success"] == true)
                return true;
            else
                return false;
        }

        public bool UpdateDomainNSGroup(DomainInfo domain, string nsGroupName)
        {
            InvalidateDomainCache();
            return request("/domains/"+domain.id, Method.PUT, out string resp, new Dictionary<string, dynamic> () 
            {
                { "ns_group", nsGroupName }
            });
        }

        public bool UpdateDomainNameServers(DomainInfo domain, List<NameServer> nameServers)
        {
            InvalidateDomainCache();
            List<Dictionary<string, string>> nsrvs = new List<Dictionary<string, string>>();

            foreach (NameServer ns in nameServers)
                nsrvs.Add(new Dictionary<string, string>() { { "name", ns.domainName }, { "ip", ns.ipv4 }, { "ip6", ns.ipv6 } });

            return request("/domains/"+domain.id, Method.PUT, out string resp, new Dictionary<string, dynamic> () 
            {
                { "ns_group", null },
                { "name_servers", nsrvs }
            });
        }

        public bool DeleteNSGroup(string groupName)
        {
            if (string.IsNullOrEmpty(groupName))
                return false;
            
            bool success = request("/dns/nameservers/groups/"+groupName, Method.DELETE, out string resp);

            if (!success)
                return false;

            Dictionary<string, object> responseJson = JsonConvert.DeserializeObject<Dictionary<string, object>>(resp);

            if (((JObject)responseJson["data"]).ToObject<Dictionary<string, bool>>()["success"] == true)
                return true;
            else
                return false;
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
            this.domainCacheExpireHours = 3;
        }

        public OP(bool useDomainCache)
        {
            this.baseUrl = "https://api.openprovider.eu/";
            this.baseVersion = "v1beta";
            this.useDomainCache = useDomainCache;
            this.domainCacheExpireHours = 3;
        }

        public OP()
        {
            this.baseUrl = "https://api.openprovider.eu/";
            this.baseVersion = "v1beta";
            this.useDomainCache = true;
            this.domainCacheExpireHours = 3;
        }
    }
}