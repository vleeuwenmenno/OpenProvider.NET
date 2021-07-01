using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenProvider.NET
{
    public class DomainParser : IDisposable
    {
        public DomainParser(string domain)
        {
            this.domain = domain;
            domainParts = domain.Split('.').ToList();
        }
        
        public string domain { get; set; }
        public List<string> domainParts { get; private set; }

        public string RegistrableDomain
        {
            get
            {
                return string.Join(".", domainParts.GetRange(domainParts.Count()-2, 2));
            }
        }
        
        public string TLD
        {
            get
            {
                return string.Join(".", domainParts.GetRange(domainParts.Count()-1, 1));
            }
        }
        
        public string[] subdomains
        {
            get
            {
                return domain.Replace(this.RegistrableDomain, "").Split('.').Where(x => !string.IsNullOrEmpty(x)).ToArray();
            }
        }

        public void Dispose()
        {
            domain = null;
        }
    }
}