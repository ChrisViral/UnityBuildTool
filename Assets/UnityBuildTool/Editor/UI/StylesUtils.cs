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
        /// Style for a centered bold label
        /// </summary>
        public static GUIStyle CenteredBoldLabel { get; }

        /// <summary>
        /// Style for the selectable user code
        /// </summary>
        public static GUIStyle UserCodeLabel { get; }

        /// <summary>
        /// Style of the connection request buttons
        /// </summary>
        public static GUIStyle ConnectionButtonStyle { get; }

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
        public static ReadOnlyDictionary<BuildAPIStatus, (GUIContent label, GUIStyle style)> ConnectionStyles { get; }
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
                fontSize  = 16,
                normal    = { textColor = Green },
                active    = { textColor = Green },
                hover     = { textColor = Green }
            };

            //Connection styles
            CenteredBoldLabel = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize  = 14,
                wordWrap = true
            };

            //Connection styles
            UserCodeLabel = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize  = 24
            };

            ConnectionButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                fontSize  = 18,
                normal    = { textColor = Color.white },
                active    = { textColor = Color.white },
                hover     = { textColor = Color.white }
            };

            //Not connected
            GUIContent notConnected = new GUIContent("Not Connected");
            float notConnectedWidth = EditorStyles.boldLabel.CalcSize(notConnected).x;

            //Error
            GUIContent error = new GUIContent("Error");
            GUIStyle errorStyle = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Red } };

            //Connected
            GUIContent connected = new GUIContent("Connected");
            GUIStyle connectedStyle = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Green } };

            //Lookup dictionary
            ConnectionStyles = new ReadOnlyDictionary<BuildAPIStatus, (GUIContent, GUIStyle)>(new Dictionary<BuildAPIStatus, (GUIContent, GUIStyle)>(3)
            {
                [BuildAPIStatus.NOT_CONNECTED] = (notConnected, EditorStyles.boldLabel),
                [BuildAPIStatus.ERROR]         = (error,        errorStyle),
                [BuildAPIStatus.CONNECTED]     = (connected,    connectedStyle)
            });

            //Release styles
            TitleLabelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize  = 20
            };

            TitleFieldStyle = new GUIStyle(EditorStyles.textField)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                fontSize  = 16
            };

            DescriptionLabelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize  = 14
            };

            DescriptionFieldStyle = new GUIStyle(EditorStyles.textArea) { fontSize = 13 };

            CenteredLabelStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };

            CenteredPopupStyle = new GUIStyle(EditorStyles.popup) { alignment = TextAnchor.MiddleCenter };

            DeleteButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                fontStyle = FontStyle.Bold,
                normal    = { textColor = Color.white },
                active    = { textColor = Color.white }
            };

            BuildButtonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize  = 24,
                normal    = { textColor = Color.white },
                active    = { textColor = Color.white }
            };
        }
        #endregion
    }
}
