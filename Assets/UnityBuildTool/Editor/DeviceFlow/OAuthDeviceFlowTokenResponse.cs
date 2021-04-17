using System.Runtime.Serialization;

namespace UnityBuildTool.DeviceFlow
{
    /// <summary>
    /// OAuth Device Flow token response
    /// </summary>
    [DataContract]
    public class OAuthDeviceFlowTokenResponse
    {
        #region Fields
        /// <summary>OAuth device access token</summary>
        [DataMember(IsRequired = false, Name = "access_token")]
        public string accessToken;
        /// <summary>OAuth access token type</summary>
        [DataMember(IsRequired = false, Name = "token_type")]
        public string tokenType;
        /// <summary>Access token scope</summary>
        [DataMember(IsRequired = false)]
        public string scope;
        /// <summary>Request error</summary>
        [DataMember(IsRequired = false)]
        public string error;
        /// <summary>Request error description</summary>
        [DataMember(IsRequired = false, Name = "error_description")]
        public string errorDescription;
        /// <summary>Request error URL</summary>
        [DataMember(IsRequired = false, Name = "error_uri")]
        public string errorURL;
        #endregion
    }
}
