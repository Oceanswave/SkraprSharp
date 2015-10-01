namespace SkraprSharp.Models
{
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a definition of a 
    /// </summary>
    public class SkraprDefinition
    {
        /// <summary>
        /// Gets or sets the address that is first loaded.
        /// </summary>
        [JsonProperty("startAddress")]
        public string StartAddress
        {
            get;
            set;
        }

        [JsonProperty("authenticationSettings")]
        public Authentication AuthenticationSettings
        {
            get;
            set;
        }

        [JsonProperty("crawlRules")]
        public List<CrawlRule> CrawlRules
        {
            get;
            set;
        }
    }
}
