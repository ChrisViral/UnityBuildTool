using UnityEngine;

namespace UnityBuildTool.UI
{
    /// <summary>
    /// GUILayout Styles utility
    /// </summary>
    public static class StylesUtils
    {
        #region Properties
        /// <summary>
        /// Red colour used throughout the UI
        /// </summary>
        public static Color Red { get; } = new Color(0.7f, 0.1f, 0.1f);

        /// <summary>
        /// Green colour used throughout the UI
        /// </summary>
        public static Color Green { get; } = new Color(0.1f, 0.6f, 0.1f);

        /// <summary>
        /// The background style of the selection UI
        /// </summary>
        public static GUIStyle BackgroundStyle { get; }
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes all the styles
        /// </summary>
        static StylesUtils()
        {
            //BackgroundStyle - Create a transparent black texture to apply shade
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.1f));
            tex.Apply();
            BackgroundStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = tex }
            };


        }
        #endregion
    }
}
