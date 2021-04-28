using System;
using UnityEditor;
using UnityEngine;
//ReSharper disable InconsistentNaming

namespace UnityBuildTool.UI.Settings
{
    [Serializable]
    public class BuildSettings
    {
        #region Constants
        /// <summary>Property name for the Standalone settings</summary>
        public const string STANDALONE_SETTINGS_NAME = nameof(standaloneSettings);
        /// <summary>Property name for the Android settings</summary>
        public const string ANDROID_SETTINGS_NAME    = nameof(androidSettings);
        /// <summary>Property name for the iOS settings</summary>
        public const string iOS_SETTINGS_NAME        = nameof(_iOSSettings);
        #endregion

        #region Fields
        private Vector2 scroll;
        private bool init;
        #endregion

        #region Properties
        [SerializeField]
        private StandaloneBuildSettings standaloneSettings = new StandaloneBuildSettings();
        /// <summary>
        /// The Standalone build settings
        /// </summary>
        public StandaloneBuildSettings StandaloneSettings => this.standaloneSettings;

        [SerializeField]
        private AndroidBuildSettings androidSettings = new AndroidBuildSettings();
        /// <summary>
        /// The Android build settings
        /// </summary>
        public AndroidBuildSettings AndroidSettings => this.androidSettings;

        [SerializeField]
        private iOSBuildSettings _iOSSettings = new iOSBuildSettings();
        /// <summary>
        /// The iOS build settings
        /// </summary>
        public iOSBuildSettings iOSSettings => this._iOSSettings;
        #endregion

        #region Methods
        public void Init()
        {
            if (!this.init)
            {
                this.init = true;
                this.StandaloneSettings.Init();
                this.AndroidSettings.Init();
                this.iOSSettings.Init();
            }
        }

        public void OnGUI()
        {
            Init();

            using (VerticalScope.Enter(GUILayout.Width(300f)))
            {
                using (BuildTargetGroupScope.Enter(out BuildTargetGroup target))
                using (ScrollViewScope.Enter(ref this.scroll, false, false, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, GUI.skin.scrollView, GUILayout.ExpandHeight(false), GUILayout.MaxHeight(210f)))
                {
                    BuildTargetGroupSettings settings = GetTargetGroupSettings(target);
                    if (settings != null)
                    {
                        settings.OnGUI();
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Platform not supported");
                    }
                }
            }
        }

        /// <summary>
        /// Gets the settings associated to the BuildTargetGroup
        /// </summary>
        /// <param name="targetGroup">Group to get the settings for</param>
        /// <returns>The associated settings, or null if not supported</returns>
        public BuildTargetGroupSettings GetTargetGroupSettings(BuildTargetGroup targetGroup)
        {
            switch (targetGroup)
            {
                case BuildTargetGroup.Standalone:
                    return this.StandaloneSettings;
                case BuildTargetGroup.Android:
                    return this.AndroidSettings;
                case BuildTargetGroup.iOS:
                    return this.iOSSettings;

                default:
                    return null;
            }
        }
        #endregion
    }
}
