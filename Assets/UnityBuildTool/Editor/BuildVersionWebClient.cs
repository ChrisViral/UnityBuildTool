using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBuildTool
{
    /// <summary>
    /// Helper methods to interact with a web client to handle build objects
    /// </summary>
    public static class BuildVersionWebClient
    {
        #region Static fields
        /// <summary>The settings to use in the JsonSerializer</summary>
        private static readonly DataContractJsonSerializerSettings settings = new DataContractJsonSerializerSettings
        {
            DateTimeFormat = new DateTimeFormat(BuildVersion.TIME_FORMAT, CultureInfo.InvariantCulture)
        };
        /// <summary>The JsonSerializer to use to serialize this type of object</summary>
        private static readonly DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(BuildVersion), settings);
        #endregion

        #region Static methods
        /// <summary>
        /// Gets a BuildVersion object from the given Library
        /// </summary>
        /// <param name="url">URL to request to</param>
        /// <returns>A task returning to object</returns>
        public static async Task<BuildVersion> GetBuildVersion(string url)
        {
            try
            {
                //Request to the client
                using (HttpResponseMessage message = await BuildToolClient.Client.GetAsync(url).ConfigureAwait(false))
                {
                    //Make sure no errors occurred
                    message.EnsureSuccessStatusCode();

                    //Get the content string
                    using (Stream stream = await message.Content.ReadAsStreamAsync())
                    {
                        return (BuildVersion)serializer.ReadObject(stream);
                    }
                }
            }
            catch (Exception e)
            {
                //Log any exceptions
                Debug.LogException(e);
            }

            //If it fails, return the default value
            return default;
        }

        /// <summary>
        /// Posts an object as Json to the given URL
        /// </summary>
        /// <param name="url">Request URL</param>
        /// <param name="version">The BuildVersion to post</param>
        public static async void PostBuildVersion(string url, BuildVersion version)
        {
            try
            {
                //Create a new stream to write the Json to
                using (MemoryStream stream = new MemoryStream())
                {
                    //Serialize the version then return to the beginning of the stream
                    serializer.WriteObject(stream, version);
                    stream.Seek(0, SeekOrigin.Begin);

                    //Create the new stream content
                    using (HttpContent content = new StreamContent(stream))
                    {
                        //Set the header
                        content.Headers.ContentType = BuildToolClient.JsonHeader;
                        //Post the content
                        using (HttpResponseMessage reply = await BuildToolClient.Client.PostAsync(url, content).ConfigureAwait(false))
                        {
                            //Make sure no errors occurred
                            reply.EnsureSuccessStatusCode();

                            //Get API reply
                            if (reply.Content != null)
                            {
                                //Print the reply
                                Debug.Log("[BuildVersionWebClient]: API Reply:\n" + await reply.Content.ReadAsStringAsync().ConfigureAwait(false));
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                //Log any exceptions
                Debug.LogException(e);
            }
        }
        #endregion
    }
}
