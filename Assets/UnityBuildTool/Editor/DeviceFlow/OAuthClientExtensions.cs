using System;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using Octokit;

namespace UnityBuildTool.DeviceFlow
{
    /// <summary>
    /// OAuth Client extensions to support Device Flow
    /// </summary>
    public static class OAuthClientExtensions
    {
        #region Constants
        /// <summary>Base GitHub login URL</summary>
        public const string API_URL = "https://github.com/login/";
        /// <summary>GitHub user code request URL path</summary>
        public const string FLOW_REQUEST_URL = API_URL + "device/code";
        /// <summary>GitHub access token poll URL</summary>
        public const string FLOW_TOKEN_URL = API_URL + "oauth/access_token";
        #endregion

        #region Static fields
        private static readonly DataContractJsonSerializer flowRequestSerializer = new DataContractJsonSerializer(typeof(OAuthDeviceFlowRequest));
        private static readonly DataContractJsonSerializer flowResponseSerializer = new DataContractJsonSerializer(typeof(OAuthDeviceFlowResponse));
        private static readonly DataContractJsonSerializer tokenRequestSerializer = new DataContractJsonSerializer(typeof(OAuthDeviceFlowTokenRequest));
        private static readonly DataContractJsonSerializer tokenResponseSerializer = new DataContractJsonSerializer(typeof(OAuthDeviceFlowTokenResponse));
        #endregion

        #region Extension methods
        /// <summary>
        /// Initiates a GitHub Device Flow OAuth request
        /// </summary>
        /// <param name="_">OAuth Client</param>
        /// <param name="request">OAuth Device Flow request</param>
        /// <returns>A task which yields the OAuth Device Flow response</returns>
        /// <exception cref="ArgumentNullException">If the passed <paramref name="request"/> is <see langword="null"/></exception>
        /// <exception cref="ApiException">If a HTTP exception occurs while communicating with the GitHub API</exception>
        public static async Task<OAuthDeviceFlowResponse> InitiateDeviceFlow(this IOauthClient _, OAuthDeviceFlowRequest request)
        {
            if (request is null) throw new ArgumentNullException(nameof(request), "Device Flow request cannot be null");

            //Create a new stream to write the Json to
            using (MemoryStream stream = new MemoryStream())
            {
                //Serialize the version then return to the beginning of the stream
                flowRequestSerializer.WriteObject(stream, request);
                stream.Seek(0, SeekOrigin.Begin);

                //Create the new stream content
                using (HttpContent content = new StreamContent(stream))
                {
                    //Set the header
                    content.Headers.ContentType = BuildToolClient.JsonHeader;
                    //Post the content
                    using (HttpResponseMessage reply = await BuildToolClient.Client.PostAsync(FLOW_REQUEST_URL, content).ConfigureAwait(false))
                    {
                        //Make sure no errors occurred
                        if (!reply.IsSuccessStatusCode)
                        {
                            throw new ApiException("HTTP Error while accessing the GitHub API", reply.StatusCode);
                        }

                        //Get the parsed response
                        return OAuthDeviceFlowResponse.FromFormString(await reply.Content.ReadAsStringAsync());
                    }
                }
            }
        }

        /// <summary>
        /// Starts polling the OAuth Device Flow access token result periodically until the user code expires or the token is available
        /// </summary>
        /// <param name="_">OAuth client</param>
        /// <param name="request">Device Flow Oauth Token request</param>
        /// <returns>The OAuth Device Flow access token</returns>
        /// <exception cref="ArgumentNullException">If the passed <paramref name="request"/> is <see langword="null"/></exception>
        /// <exception cref="ApiException">If a HTTP exception occurs while communicating with the GitHub API, or if the access token retrieval leads to an unexpected error message</exception>
        /// <exception cref="TimeoutException">If the user code expires before the access token can be retrieved</exception>
        public static async Task<OAuthDeviceFlowTokenResponse> PollDeviceFlowAccessTokenResult(this IOauthClient _, OAuthDeviceFlowTokenRequest request)
        {
            if (request is null) throw new ArgumentNullException(nameof(request), "Device Flow token request cannot be null");

            //Create a new stream to write the Json to
            using (MemoryStream stream = new MemoryStream())
            {
                //Serialize the version then return to the beginning of the stream
                tokenRequestSerializer.WriteObject(stream, request);
                stream.Seek(0, SeekOrigin.Begin);

                //Create the new stream content
                using (HttpContent content = new StreamContent(stream))
                {
                    //Set the header
                    content.Headers.ContentType = BuildToolClient.JsonHeader;

                    do
                    {
                        await Task.Delay(request.pollRate);

                        //Post the content
                        using (HttpResponseMessage reply = await BuildToolClient.Client.PostAsync(FLOW_TOKEN_URL, content).ConfigureAwait(false))
                        {
                            //Make sure no errors occurred
                            if (!reply.IsSuccessStatusCode)
                            {
                                throw new ApiException("HTTP Error while accessing the GitHub API", reply.StatusCode);
                            }

                            OAuthDeviceFlowTokenResponse response = OAuthDeviceFlowTokenResponse.FromFormString(await reply.Content.ReadAsStringAsync());

                            if (response.error is null)
                            {
                                return response;
                            }

                            switch (response.error)
                            {
                                case "authorization_pending":
                                    break;

                                case "slow_down":
                                    request.pollRate += TimeSpan.FromSeconds(5);
                                    break;

                                case "expired token":
                                    throw new TimeoutException("OAuth user code has expired, a new one must be requested");

                                default:
                                    throw new ApiException($"{response.error}: {response.errorDescription}\n{response.errorURL}", reply.StatusCode);
                            }
                        }
                    }
                    while (DateTime.Now < request.expiry);

                    throw new TimeoutException("OAuth user code has expired, a new one must be requested");
                }
            }
        }
        #endregion
    }
}
