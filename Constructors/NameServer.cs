using System.Collections.Generic;

namespace OpenProvider.NET
{
    public class NameServer
    {
        public long id {get; set;}
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
}