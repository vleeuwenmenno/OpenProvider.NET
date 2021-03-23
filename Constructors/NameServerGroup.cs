using System.Collections.Generic;

namespace OpenProvider.NET
{
    public class NameServerGroup
    {
        public long id {get; set;}
        public string name {get; set;}
        public List<NameServer> nameServers {get;set;}

        public NameServerGroup()
        {
            nameServers = new List<NameServer>();
        }
    }
}