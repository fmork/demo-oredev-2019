using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;

namespace demunity.lib.Security
{
    public class InMemoryXmlRepository : IXmlRepository
    {
        private readonly List<DataProtectionKey> inMemoryKeys = new List<DataProtectionKey>();
        public IReadOnlyCollection<XElement> GetAllElements()
        {
            return inMemoryKeys
                .Select(key => XElement.Parse(key.Xml))
                .ToList()
                .AsReadOnly();
        }

        public void StoreElement(XElement element, string friendlyName)
        {
            inMemoryKeys.Add(new DataProtectionKey
            {
                Id = Guid.NewGuid(),
                FriendlyName = friendlyName,
                Xml = element.ToString()
            });
        }
    }
}