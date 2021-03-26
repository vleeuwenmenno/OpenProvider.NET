using System.Collections.Generic;
using System.Net;

namespace OpenProvider.NET
{
    public class NameServer
    {
        public long id {get; set;}
        public long seqNo {get; set;}
        public string domainName {get; set;}
        public string ipv4 {get; set;}
        public string ipv6 {get; set;}

        public bool ResolveIPs()
        {
            if (!string.IsNullOrEmpty(this.domainName))
            {
                IPAddress[] ips = Dns.GetHostAddresses(domainName);
                
                if (ips.Length == 0)
                    return false;
                
                if (ips.Length == 2)
                    this.ipv6 =  ips[1].ToString();
                    
                this.ipv4 =  ips[0].ToString();
                return true;
            }
            return false;
        }

        public NameServer() { }

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
}