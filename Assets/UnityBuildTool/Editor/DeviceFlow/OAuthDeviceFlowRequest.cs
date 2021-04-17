using System.Runtime.Serialization;

namespace UnityBuildTool.DeviceFlow
{
    /// <summary>
    /// Creates a new OAuth Device Flow request object
    /// </summary>
    [DataContract]
    public class OAuthDeviceFlowRequest
    {
        #region Constants
        private const string defaultScope = "user";
        #endregion

        #region Fields
        /// <summary>App client ID</summary>
        [DataMember(IsRequired = true, Order = 0, Name = "client_id")]
        public string clientID;
        /// <summary>Requested scopes</summary>
        [DataMember(IsRequired = true, Order = 1)]
        public string scope = defaultScope;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new OAuth device flow request with the specified client ID and default scope (user)
        /// </summary>
        /// <param name="clientID">Application client ID</param>
        public OAuthDeviceFlowRequest(string clientID) => this.clientID = clientID;

        /// <summary>
        /// Creates a new OAuth device flow request with the specified client ID and scope
        /// </summary>
        /// <param name="clientID">Application client ID</param>
        /// <param name="scopes">Requested scope</param>
        public OAuthDeviceFlowRequest(string clientID, params string[] scopes) : this(clientID) => this.scope = string.Join(", ", scopes);
        #endregion
    }
}
