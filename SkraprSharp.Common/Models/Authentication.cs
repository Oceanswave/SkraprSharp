namespace SkraprSharp.Models
{
    using Newtonsoft.Json;

    /// <summary>
    /// Represents settings associated with authentication
    /// </summary>
    public class Authentication
    {
        /// <summary>
        /// Gets or sets the script that is executed to determine if the authentication needs to be performed.
        /// </summary>
        [JsonProperty("authenticationTest")]
        public string AuthenticationTest
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the path to the script that is executed when authentication is required. 
        /// </summary>
        public string AuthenticationScriptPath
        {
            get;
            set;
        }
    }
}
