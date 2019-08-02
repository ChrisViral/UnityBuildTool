using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace BuildTool
{
    /// <summary>
    /// Helper methods to interact with a web client to handle build objects
    /// </summary>
    public static class JsonWebClient
    {
        #region Instance
        /// <summary>
        /// HTTP Client instance
        /// </summary>
        private static readonly HttpClient client = new HttpClient();
        #endregion

        #region Static methods
        /// <summary>
        /// Gets an object as Json from the given URL
        /// </summary>
        /// <typeparam name="T">Type of object to load</typeparam>
        /// <param name="url">URL to request to</param>
        /// <returns>A task returning to object</returns>
        public static async Task<T> GetJsonObject<T>(string url)
        {
            try
            {
                //Request to the client
                using (HttpResponseMessage message = await client.GetAsync(url))
                {
                    //Make sure no errors occured
                    message.EnsureSuccessStatusCode();

                    //Get the content string
                    string content = await message.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<T>(content);
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
        /// <typeparam name="T">Type of object to put</typeparam>
        /// <param name="url">Request URL</param>
        /// <param name="obj">The object to put</param>
        public static async void PostJsonObject<T>(string url, T obj)
        {
            try
            {
                //Encode the content as Json
                using (HttpContent content = new StringContent(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json"))
                using (HttpResponseMessage reply = await client.PostAsync(url, content))
                {
                    //Make sure no errors occured
                    reply.EnsureSuccessStatusCode();

                    //Get API reply
                    if (reply.Content != null)
                    {
                        //Print the reply
                        Debug.Log("[JsonWebClient]: API Reply:\n" + await reply.Content.ReadAsStringAsync());
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
