
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenProvider.NET
{
    public class Domain
    {
        public string domainName {get; set;}
        public string extension {get; set;}

        public Domain() {}

        public Domain(Dictionary<string, string> raw)
        {
            this.domainName = raw["name"];
            this.extension = raw["extension"];
        }
    }

    public class NameServer
    {
        public long seqNo {get; set;}
        public string domainName {get; set;}
        public string ipv4 {get; set;}
        public string ipv6 {get; set;}

        public NameServer() {}
        public NameServer(Dictionary<string, string> nameServer)
        {
            this.seqNo = long.Parse(nameServer["seq_nr"]);
            this.domainName = nameServer["name"];

            if (nameServer.ContainsKey("ip"))
                this.ipv6 = nameServer["ip"];

            if (nameServer.ContainsKey("ip6"))
                this.ipv6 = nameServer["ip6"];
        }
    }

    public enum DNSSecMode
    {
        Unsigned = 0,
        SignedDelegation = 1
    }

    public enum AutoRenew
    {
        Default = 0,
        On = 1,
        Off = 1,
    }

    public class DomainInfo
    {
        public long id {get; set;}
        public long reseller {get; set;}
        public long nameserverGroupId {get; set;}

        public bool canRenew {get; set;}

        public string authCode {get; set;}

        public Domain domain {get; set;}
        public List<NameServer> nameservers {get; set;}
        public string nsGroupName {get; set;}

        public DNSSecMode dnsSec {get; set;}
        public AutoRenew autoRenew {get; set;}

        public DateTime creationDate {get; set;}
        public DateTime lastChanged {get; set;}
        public DateTime orderDate {get; set;}
        public DateTime expirationDate {get; set;}
        public DateTime registryExpirationDate {get; set;}
        public DateTime renewalDate {get; set;}

        public DomainInfo() { }

        public DomainInfo(Dictionary<string, object> rawData)
        {
            foreach (KeyValuePair<string, object> domain in rawData)
            {
                switch (domain.Key)
                {
                    case "id":
                        this.id = (long)domain.Value;
                        continue;

                    case "reseller_id":
                        this.reseller = (long)domain.Value;
                        continue;

                    case "can_renew":
                        this.canRenew = (bool)domain.Value;
                        continue;

                    case "auth_code":
                        this.authCode = (string)domain.Value;
                        continue;

                    case "ns_group":
                        this.nsGroupName = (string)domain.Value;
                        continue;

                    case "domain":
                        this.domain = new Domain(((Newtonsoft.Json.Linq.JObject)domain.Value).ToObject<Dictionary<string, string>>());
                        continue;

                    case "name_servers":
                        this.nameservers = (((Newtonsoft.Json.Linq.JArray)domain.Value).ToObject<List<Dictionary<string, string>>>()).Select(x => new NameServer(x)).ToList();
                        continue;

                    case "dnssec":
                        this.dnsSec = (string)domain.Value == "unsigned" ? DNSSecMode.Unsigned : DNSSecMode.SignedDelegation;
                        continue;

                    case "autorenew":
                        this.autoRenew = (string)domain.Value == "default" ? AutoRenew.Default : (string)domain.Value == "on" ? AutoRenew.On : AutoRenew.Off;
                        continue;

                    case "creation_date":
                        this.creationDate = DateTime.Parse((string)domain.Value);
                        continue;

                    case "last_changed":
                        this.lastChanged = DateTime.Parse((string)domain.Value);
                        continue;

                    case "order_date":
                        this.orderDate = DateTime.Parse((string)domain.Value);
                        continue;

                    case "expiration_date":
                        this.expirationDate = DateTime.Parse((string)domain.Value);
                        continue;

                    case "registry_expiration_date":
                        this.registryExpirationDate = DateTime.Parse((string)domain.Value);
                        continue;

                    case "renewal_date":
                        this.renewalDate = DateTime.Parse((string)domain.Value);
                        continue;
                }
                
            }
        }
    } 
}