using System.Runtime.Serialization;

namespace UnityBuildTool.DeviceFlow
{
    /// <summary>
    /// OAuth Device Flow initialization response
    /// </summary>
    [DataContract]
    public class OAuthDeviceFlowResponse
    {
        #region Fields
        /// <summary>The device verification code</summary>
        [DataMember(IsRequired = true, Order = 0, Name = "device_code")]
        public string deviceCode;
        /// <summary>The user code to input</summary>
        [DataMember(IsRequired = true, Order = 1, Name = "user_code")]
        public string userCode;
        /// <summary>URL at which the user code must be entered</summary>
        [DataMember(IsRequired = true, Order = 2, Name = "verification_uri")]
        public string verificationUrl;
        /// <summary>Expiring time of the user code in seconds</summary>
        [DataMember(IsRequired = true, Order = 3, Name = "expires_in")]
        public int expiry;
        /// <summary>Minimum verification poll rate in seconds</summary>
        [DataMember(IsRequired = true, Order = 4, Name = "interval")]
        public int pollRate;
        #endregion
    }
}
