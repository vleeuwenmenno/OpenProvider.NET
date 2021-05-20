using System.Collections.Generic;

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

        public string fullDomain
        {
            get
            {
                return domainName + "." + extension;
            }
        }
    }
}