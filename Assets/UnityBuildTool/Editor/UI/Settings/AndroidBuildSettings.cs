using System;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine;

namespace UnityBuildTool.UI.Settings
{
    [Serializable]
    public class AndroidBuildSettings : BuildTargetGroupSettings
    {
        #region Constants
        //Labels
        private static readonly GUIContent textureCompressionLabel = new GUIContent("Texture Compression", "The used texture compression algorithm.");
        private static readonly GUIContent etc2FallbackLabel       = new GUIContent("ETC2 Fallback",       "ETC2 Fallback compression.");
        private static readonly GUIContent exportProjectLabel      = new GUIContent("Export Project",      "Export the Project as a Gradle project that you can import into Android Studio.");
        private static readonly GUIContent symlinkSourcesLabel     = new GUIContent("Symlink Sources",     "Directly reference Java or Kotlin sources from Unity.");
        private static readonly GUIContent buildAppBundleLabel     = new GUIContent("Build App Bundle",    "Build an Android App Bundle for distribution on Google Play.");

        //Enum values
        private static readonly string[] textureCompressionValues =
        {
            "Don't override",
            "ETC (default)",
            "ETC2 (GLES 3.0)",
            "ASTC",
            "DXT (Tegra)",
            "PVRTC (PowerVR)"
        };
        private static readonly string[] fallbackValues =
        {
            "32-bit",
            "16-bit",
            "32-bit, half resolution"
        };
        #endregion

        #region Properties
        /// <summary>
        /// Name for the serialized settings
        /// </summary>
        public override string SerializedName { get; } = BuildSettings.ANDROID_SETTINGS_NAME;

        /// <summary>
        /// Texture compression popup index
        /// </summary>
        private int TextureCompressionIndex
        {
            get
            {
                switch (EditorUserBuildSettings.androidBuildSubtarget)
                {
                    case MobileTextureSubtarget.Generic:
                        return 0;
                    case MobileTextureSubtarget.ETC:
                        return 1;
                    case MobileTextureSubtarget.ETC2:
                        return 2;
                    case MobileTextureSubtarget.ASTC:
                        return 3;
                    case MobileTextureSubtarget.DXT:
                        return 4;
                    case MobileTextureSubtarget.PVRTC:
                        return 5;

                    default:
                        return 0;
                }
            }
            set
            {
                switch (value)
                {
                    case 0:
                        EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.Generic;
                        break;
                    case 1:
                        EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ETC;
                        break;
                    case 2:
                        EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ETC2;
                        break;
                    case 3:
                        EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
                        break;
                    case 4:
                        EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.DXT;
                        break;
                    case 5:
                        EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.PVRTC;
                        break;

                    default:
                        EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.Generic;
                        break;
                }
            }
        }

        /// <summary>
        /// ETC2 fallback index
        /// </summary>
        private int FallbackIndex
        {
            get
            {
                switch (EditorUserBuildSettings.androidETC2Fallback)
                {
                    case AndroidETC2Fallback.Quality32Bit:
                        return 0;
                    case AndroidETC2Fallback.Quality16Bit:
                        return 1;
                    case AndroidETC2Fallback.Quality32BitDownscaled:
                        return 2;

                    default:
                        return 0;
                }
            }
            set
            {
                switch (value)
                {
                    case 0:
                        EditorUserBuildSettings.androidETC2Fallback = AndroidETC2Fallback.Quality32Bit;
                        break;
                    case 1:
                        EditorUserBuildSettings.androidETC2Fallback = AndroidETC2Fallback.Quality16Bit;
                        break;
                    case 2:
                        EditorUserBuildSettings.androidETC2Fallback = AndroidETC2Fallback.Quality32BitDownscaled;
                        break;

                    default:
                        EditorUserBuildSettings.androidETC2Fallback = AndroidETC2Fallback.Quality32Bit;
                        break;
                }
            }
        }
        #endregion

        #region Methods
        public override void OnGUI()
        {
            this.TextureCompressionIndex                         = EditorGUILayout.Popup(textureCompressionLabel, this.TextureCompressionIndex, textureCompressionValues);
            this.FallbackIndex                                   = EditorGUILayout.Popup(etc2FallbackLabel,       this.FallbackIndex,           fallbackValues);
            EditorUserBuildSettings.exportAsGoogleAndroidProject = EditorGUILayout.Toggle(exportProjectLabel,  EditorUserBuildSettings.exportAsGoogleAndroidProject);
            UserBuildSettings.symlinkSources                     = EditorGUILayout.Toggle(symlinkSourcesLabel, UserBuildSettings.symlinkSources);
            EditorUserBuildSettings.buildAppBundle               = EditorGUILayout.Toggle(buildAppBundleLabel, EditorUserBuildSettings.buildAppBundle);
            base.OnGUI();
        }
        #endregion
    }
}
