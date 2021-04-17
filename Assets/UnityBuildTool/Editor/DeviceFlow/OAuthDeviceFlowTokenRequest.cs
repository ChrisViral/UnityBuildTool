using System;
using System.Runtime.Serialization;

namespace UnityBuildTool.DeviceFlow
{
    /// <summary>
    /// OAuth Device Flow token request
    /// </summary>
    [DataContract]
    public class OAuthDeviceFlowTokenRequest
    {
        #region Constants
        /// <summary>Device Flow default grant type</summary>
        public const string DEVICE_FLOW_GRANT = "urn:ietf:params:oauth:grant-type:device_code";
        #endregion

        #region Fields
        /// <summary>Application client ID</summary>
        [DataMember(IsRequired = true, Order = 0, Name = "client_id")]
        public string clientID;
        /// <summary>OAuth device code</summary>
        [DataMember(IsRequired = true, Order = 1, Name = "device_code")]
        public string deviceCode;
        /// <summary>Token grant type</summary>
        [DataMember(IsRequired = true, Order = 2, Name = "grant_type")]
        public string grantType = DEVICE_FLOW_GRANT;
        /// <summary> Request expiry</summary>
        public DateTime expiry = DateTime.Now + TimeSpan.FromSeconds(900);
        /// <summary>Minimum time for success polling</summary>
        public TimeSpan pollRate = TimeSpan.FromSeconds(5);
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new OAuth Device Flow token request with the specified client ID and device code
        /// </summary>
        /// <param name="clientID">This application's client ID</param>
        /// <param name="deviceCode">Current OAuth request device code</param>
        public OAuthDeviceFlowTokenRequest(string clientID, string deviceCode)
        {
            this.clientID = clientID;
            this.deviceCode = deviceCode;
        }
        #endregion
    }
}
