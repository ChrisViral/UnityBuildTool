using System;
using UnityEditor;
using UnityEngine;

namespace UnityBuildTool.UI.Settings
{
    [Serializable]
    public abstract class BuildTargetGroupSettings
    {
        #region Constants
        private static readonly GUIContent developmentBuildLabel    = new GUIContent("Development Build",         "Enables development build options.");
        private static readonly GUIContent autoconnectProfilerLabel = new GUIContent("Autoconnect Profiler",      "When the build is started, it automatically attempts to connect to the profiler window.");
        private static readonly GUIContent deepProfilingLabel       = new GUIContent("Deep Profiling Support",    "Build player with deep profiling support. This might affect player performance.");
        private static readonly GUIContent scriptDebuggingLabel     = new GUIContent("Script Debugging",          "Enable this setting to allow your script code to be debugged.");
        private static readonly GUIContent waitForDebuggerLabel     = new GUIContent("Wait For Managed Debugger", "Allows attaching a script debugger before any script execution.");
        private static readonly GUIContent scriptsOnlyLabel         = new GUIContent("Scripts Only Build",        "Only recompiles scripts and skips processing of assets. The final build will use the previously built assets.");
        private static readonly GUIContent compressionMethodLabel   = new GUIContent("Compression Method",        "Compression applied to player data.");

        private static readonly GUIContent[] compressionLevelLabels     =
        {
            new GUIContent("Default", "None or default platform compression"),
            new GUIContent("LZ4", "Fast compression suitable for development builds"),
            new GUIContent("LZ4HC", "Higher compression than LZ4, causes longer builds and works better for release builds.")
        };
        #endregion

        #region Fields
        //Serialized values
        public bool developmentBuild;
        public bool autoconnectProfiler;
        public bool deepProfiling;
        public bool scriptDebugging;
        public bool waitForDebugger;
        public bool scriptsOnly;
        public CompressionType compressionMethod;

        //Properties
        private SerializedProperty developmentBuildProperty;
        private SerializedProperty autoconnectProfilerProperty;
        private SerializedProperty deepProfilingProperty;
        private SerializedProperty scriptDebuggingProperty;
        private SerializedProperty waitForDebuggerProperty;
        private SerializedProperty scriptsOnlyProperty;
        private SerializedProperty compressionMethodProperty;
        #endregion

        #region Properties
        /// <summary>
        /// BuildOptions for this TargetGroup
        /// </summary>
        public virtual BuildOptions Options
        {
            get
            {
                BuildOptions options = BuildOptions.None;
                if (this.developmentBuild)
                {
                    options |= BuildOptions.Development;
                    if (this.autoconnectProfiler)
                    {
                        options |= BuildOptions.ConnectWithProfiler;
                    }

                    if (this.deepProfiling)
                    {
                        options |= BuildOptions.EnableDeepProfilingSupport;
                    }

                    if (this.scriptDebugging)
                    {
                        options |= BuildOptions.AllowDebugging;
                    }

                    if (this.scriptsOnly)
                    {
                        options |= BuildOptions.BuildScriptsOnly;
                    }

                    switch (this.compressionMethod)
                    {
                        case CompressionType.Lz4:
                            options |= BuildOptions.CompressWithLz4;
                            break;

                        case CompressionType.Lz4HC:
                            options |= BuildOptions.CompressWithLz4HC;
                            break;
                    }
                }

                return options;
            }
        }

        /// <summary>
        /// Index for the CompressionMethod
        /// </summary>
        public int CompressionMethodIndex
        {
            get
            {
                switch ((CompressionType)this.compressionMethodProperty.intValue)
                {
                    case CompressionType.None:
                        return 0;
                    case CompressionType.Lz4:
                        return 1;
                    case CompressionType.Lz4HC:
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
                        this.compressionMethodProperty.intValue = (int)CompressionType.None;
                        break;
                    case 1:
                        this.compressionMethodProperty.intValue = (int)CompressionType.Lz4;
                        break;
                    case 2:
                        this.compressionMethodProperty.intValue = (int)CompressionType.Lz4HC;
                        break;

                    default:
                        this.compressionMethodProperty.intValue = (int)CompressionType.None;
                        break;
                }
            }
        }

        /// <summary>
        /// Name of the serialized property holding this instance
        /// </summary>
        public abstract string SerializedName { get; }
        #endregion

        #region Methods
        public virtual void Init()
        {
            SerializedProperty settings = BuildToolWindow.Window.SerializedSettings.FindProperty(BuildToolSettings.BUILD_SETTINGS_NAME).FindPropertyRelative(this.SerializedName);
            this.developmentBuildProperty       = settings.FindPropertyRelative(nameof(this.developmentBuild));
            this.autoconnectProfilerProperty    = settings.FindPropertyRelative(nameof(this.autoconnectProfiler));
            this.deepProfilingProperty          = settings.FindPropertyRelative(nameof(this.deepProfiling));
            this.scriptDebuggingProperty        = settings.FindPropertyRelative(nameof(this.scriptDebugging));
            this.waitForDebuggerProperty        = settings.FindPropertyRelative(nameof(this.waitForDebugger));
            this.scriptsOnlyProperty            = settings.FindPropertyRelative(nameof(this.scriptsOnly));
            this.compressionMethodProperty      = settings.FindPropertyRelative(nameof(this.compressionMethod));
        }

        public virtual void OnGUI()
        {
            bool uiEnabled = !BuildToolWindow.Window || BuildToolWindow.Window.UIEnabled;
            EditorGUILayout.PropertyField(this.developmentBuildProperty, developmentBuildLabel);

            GUI.enabled = uiEnabled && this.developmentBuildProperty.boolValue;
            EditorGUILayout.PropertyField(this.autoconnectProfilerProperty, autoconnectProfilerLabel);
            EditorGUILayout.PropertyField(this.deepProfilingProperty, deepProfilingLabel);
            EditorGUILayout.PropertyField(this.scriptDebuggingProperty, scriptDebuggingLabel);

            GUI.enabled = uiEnabled && this.developmentBuildProperty.boolValue && this.scriptDebuggingProperty.boolValue;
            EditorGUILayout.PropertyField(this.waitForDebuggerProperty, waitForDebuggerLabel);

            GUI.enabled = uiEnabled && this.developmentBuildProperty.boolValue;
            EditorGUILayout.PropertyField(this.scriptsOnlyProperty, scriptsOnlyLabel);
            this.CompressionMethodIndex = EditorGUILayout.Popup(compressionMethodLabel, this.CompressionMethodIndex, compressionLevelLabels);

            GUI.enabled = BuildToolWindow.Window.UIEnabled;
        }
        #endregion
    }
}
