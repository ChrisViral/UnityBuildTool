using System.Net.Http;
using System.Net.Http.Headers;

namespace UnityBuildTool
{
    /// <summary>
    /// App wide HTTP client objects
    /// </summary>
    public static class BuildToolClient
    {
        #region Static properties
        /// <summary>
        /// HTTP Client instance for the entire app
        /// </summary>
        public static HttpClient Client { get; } = new HttpClient();

        ///<summary>
        /// JSON app media REST header
        /// </summary>
        public static MediaTypeHeaderValue JsonHeader { get; } = new MediaTypeHeaderValue("application/json");
        #endregion
    }
}
