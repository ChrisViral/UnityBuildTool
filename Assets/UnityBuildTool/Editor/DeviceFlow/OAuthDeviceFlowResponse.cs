using System;

namespace UnityBuildTool.DeviceFlow
{
    /// <summary>
    /// OAuth Device Flow initialization response
    /// </summary>
    public class OAuthDeviceFlowResponse
    {
        #region Fields
        /// <summary>The device verification code</summary>
        public string deviceCode;
        /// <summary>The user code to input</summary>
        public string userCode;
        /// <summary>URL at which the user code must be entered</summary>
        public string verificationUrl;
        /// <summary>Expiring time of the user code in seconds</summary>
        public int expiry;
        /// <summary>Minimum verification poll rate in seconds</summary>
        public int pollRate;
        #endregion

        #region Constructors
        /// <summary>
        /// Forces usage of static factory method
        /// </summary>
        private OAuthDeviceFlowResponse() { }
        #endregion

        #region Static methods
        /// <summary>
        /// Parses an OAuth Device Flow response from an Form URL Encoded object
        /// </summary>
        /// <param name="form">Form URL Encoded string</param>
        /// <returns>The resulting OAuth Device Flow response</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="form"/> is <see langword="null"/></exception>
        public static OAuthDeviceFlowResponse FromFormString(string form)
        {
            if (form == null) throw new ArgumentNullException(nameof(form), "Form string cannot be null");

            OAuthDeviceFlowResponse response = new OAuthDeviceFlowResponse();
            foreach (string parameter in form.Split('&'))
            {
                string[] pair = parameter.Split('=');
                if (pair.Length == 2)
                {
                    string value = Uri.UnescapeDataString(pair[1]);
                    switch (pair[0])
                    {
                        case "device_code":
                            response.deviceCode = value;
                            break;

                        case "user_code":
                            response.userCode = value;
                            break;

                        case "verification_uri":
                            response.verificationUrl = value;
                            break;

                        case "expires_in":
                            response.expiry = int.Parse(value);
                            break;

                        case "interval":
                            response.pollRate = int.Parse(value);
                            break;
                    }
                }
            }

            return response;
        }
        #endregion
    }
}
