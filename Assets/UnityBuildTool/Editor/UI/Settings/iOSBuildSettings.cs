using System;
using UnityEditor;
using UnityEngine;

namespace UnityBuildTool.UI.Settings
{
    //ReSharper disable once InconsistentNaming
    [Serializable]
    public class iOSBuildSettings : BuildTargetGroupSettings
    {
        #region Constants
        //Labels
        private static readonly GUIContent runAsLabel = new GUIContent("Run in Xcode as", "Select whether Xcode runs your Project as a Release or Debug build.");
        #endregion

        #region Properties
        /// <summary>
        /// Name for the serialized settings
        /// </summary>
        public override string SerializedName { get; } = BuildSettings.iOS_SETTINGS_NAME;
        #endregion

        #region Methods
        public override void OnGUI()
        {
            EditorUserBuildSettings.iOSBuildConfigType = (iOSBuildType)EditorGUILayout.EnumPopup(runAsLabel, EditorUserBuildSettings.iOSBuildConfigType);
            base.OnGUI();
        }
        #endregion
    }
}
