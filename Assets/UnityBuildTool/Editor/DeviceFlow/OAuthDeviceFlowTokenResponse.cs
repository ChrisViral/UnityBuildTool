using System;
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

        /// <summary>
        /// Parses an OAuth Device Flow token response from an Form URL Encoded object
        /// </summary>
        /// <param name="form">Form URL Encoded string</param>
        /// <returns>The resulting OAuth Device Flow token response</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="form"/> is <see langword="null"/></exception>
        public static OAuthDeviceFlowTokenResponse FromFormString(string form)
        {
            if (form == null) throw new ArgumentNullException(nameof(form), "Form string cannot be null");

            OAuthDeviceFlowTokenResponse response = new OAuthDeviceFlowTokenResponse();
            foreach (string parameter in form.Split('&'))
            {
                string[] pair = parameter.Split('=');
                if (pair.Length == 2)
                {
                    string value = Uri.UnescapeDataString(pair[1]);
                    switch (pair[0])
                    {
                        case "access_token":
                            response.accessToken = value;
                            break;

                        case "token_type":
                            response.tokenType = value;
                            break;

                        case "scope":
                            response.scope = value;
                            break;

                        case "error":
                            response.error = value;
                            break;

                        case "error_description":
                            response.errorDescription = value;
                            break;

                        case "error_uri":
                            response.errorURL = value;
                            break;
                    }
                }
            }

            return response;
        }
    }
}
