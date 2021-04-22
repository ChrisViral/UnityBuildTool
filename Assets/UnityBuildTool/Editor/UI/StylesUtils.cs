using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEditor;
using UnityEngine;
using BuildAPIStatus = UnityBuildTool.UI.BuildToolWindow.BuildAPIStatus;

namespace UnityBuildTool.UI
{
    /// <summary>
    /// GUILayout Styles utility
    /// </summary>
    public static class StylesUtils
    {
        #region Colors
        /// <summary>
        /// Red colour used throughout the UI
        /// </summary>
        public static Color Red { get; } = new Color(0.7f, 0.1f, 0.1f);

        /// <summary>
        /// Green colour used throughout the UI
        /// </summary>
        public static Color Green { get; } = new Color(0.1f, 0.6f, 0.1f);
        #endregion

        #region Styles
        /// <summary>
        /// Style of the large Refresh button
        /// </summary>
        public static GUIStyle RefreshButtonStyle { get; }

        /// <summary>
        /// Style of the title label of the build handler
        /// </summary>
        public static GUIStyle TitleLabelStyle { get; }

        /// <summary>
        /// Style of the title field of the build handler
        /// </summary>
        public static GUIStyle TitleFieldStyle { get; }

        /// <summary>
        /// Style of the description field of the build handler
        /// </summary>
        public static GUIStyle DescriptionLabelStyle { get; }

        /// <summary>
        /// Style of the description label of the build handler
        /// </summary>
        public static GUIStyle DescriptionFieldStyle { get; }

        /// <summary>
        /// Centered label style
        /// </summary>
        public static GUIStyle CenteredLabelStyle { get; }

        /// <summary>
        /// Centered popup menu style
        /// </summary>
        public static GUIStyle CenteredPopupStyle { get; }

        /// <summary>
        /// Delete button style for the files/folders to copy sub window
        /// </summary>
        public static GUIStyle DeleteButtonStyle { get; }

        /// <summary>
        /// BuildHandler build button style
        /// </summary>
        public static GUIStyle BuildButtonStyle { get; }

        /// <summary>
        /// Array containing the GUIStyles of labels indicating the connection of the webservice
        /// </summary>
        public static ReadOnlyDictionary<BuildAPIStatus, (string, GUIStyle)> ConnectionStyles { get; }
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes all the styles
        /// </summary>
        static StylesUtils()
        {
            //RefreshButtonStyle - Large button with green text
            RefreshButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 16,
                normal = { textColor = Green },
                active = { textColor = Green }
            };

            //Connection label styles
            ConnectionStyles = new ReadOnlyDictionary<BuildAPIStatus, (string, GUIStyle)>(new Dictionary<BuildAPIStatus, (string, GUIStyle)>(3)
            {
                [BuildAPIStatus.NOT_CONNECTED] = ("Not Connected", EditorStyles.boldLabel),
                [BuildAPIStatus.ERROR] =         ("Error",         new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Red } }),
                [BuildAPIStatus.CONNECTED] =     ("Connected",     new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Green } })
            });

            //Release styles
            TitleLabelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleLeft
            };

            TitleFieldStyle = new GUIStyle(EditorStyles.textField)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Bold
            };

            DescriptionLabelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft

            };

            DescriptionFieldStyle = new GUIStyle(EditorStyles.textArea) { fontSize = 13 };

            CenteredLabelStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };

            CenteredPopupStyle = new GUIStyle(EditorStyles.popup) { alignment = TextAnchor.MiddleCenter };

            DeleteButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
                active = { textColor = Color.white }
            };

            BuildButtonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 24,
                normal = { textColor = Color.white },
                active = { textColor = Color.white }
            };
        }
        #endregion
    }
}
