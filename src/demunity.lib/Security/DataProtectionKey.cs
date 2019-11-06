using System;

namespace demunity.lib.Security
{
    /// <summary>
    /// Code first model used by <see cref="EntityFrameworkCoreXmlRepository{TContext}"/>.
    /// </summary>
    public class DataProtectionKey
    {
        /// <summary>
        /// The entity identifier of the <see cref="DataProtectionKey"/>.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The friendly name of the <see cref="DataProtectionKey"/>.
        /// </summary>
        public string FriendlyName { get; set; }

        /// <summary>
        /// The XML representation of the <see cref="DataProtectionKey"/>.
        /// </summary>
        public string Xml { get; set; }
    }
}