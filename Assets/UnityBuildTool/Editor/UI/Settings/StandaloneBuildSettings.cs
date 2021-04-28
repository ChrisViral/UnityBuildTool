using System;
using System.Linq;
using UnityBuildTool.Extensions;
using UnityEditor;
using UnityEngine;
using WindowsUserBuildSettings = UnityEditor.WindowsStandalone.UserBuildSettings;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.OSXStandalone;
using OSXUserBuildSettings = UnityEditor.OSXStandalone.UserBuildSettings;
#endif

namespace UnityBuildTool.UI.Settings
{
    [Serializable]
    public class StandaloneBuildSettings : BuildTargetGroupSettings
    {
        #region Constants
        /// <summary>Max length for the displayed path in the to copy window</summary>
        private const int MAX_PATH_LENGTH = 25;

        private static readonly GUIContent serverBuildLabel        = new GUIContent("Server Build",                  "Headless player build tailored for server environments.");
        private static readonly GUIContent copyPDBLabel            = new GUIContent("Copy PDB files",                "Copy PDB files containing debugging symbols to the final destination.");
        private static readonly GUIContent createVSSolutionLabel   = new GUIContent("Create Visual Studio Solution", "Generates Visual Studio solution files for the project.");
        private static readonly GUIContent createXcodeProjectLabel = new GUIContent("Create Xcode Project",          "Creates the Xcode Project for this Unity project.");
        #if UNITY_2020_2_OR_NEWER
        private static readonly GUIContent architectureLabel       = new GUIContent("Architecture", "Target processor architecture for MaxOS builds.");
        #endif
        private static readonly GUIContent copyLabel   = new GUIContent("Files/Folders to copy", "Files and folders that should be copied over when building the game");
        private static readonly GUIContent deleteEntry = new GUIContent("X",                     "Delete this entry to copy on build");

        //Options arrays
        private static readonly GUILayoutOption[] deleteButtonOptions = { GUILayout.Width(20f) };
        private static readonly GUILayoutOption[] copyPopupOptions    = { GUILayout.Width(100f) };
        private static readonly GUILayoutOption[] addButtonOptions    = { GUILayout.Width(90f) };
        #if UNITY_2020_2_OR_NEWER
        private static readonly string[] architectureValues =
        {
            "Intel 64-bit",
            "Apple silicon",
            "Intel 64-bit + Apple silicon"
        };
        private static readonly MacOSArchitecture[] architectures =
        {
            MacOSArchitecture.x64,
            MacOSArchitecture.ARM64,
            MacOSArchitecture.x64ARM64
        };
        #endif
        #endregion

        #region Fields
        private bool init;
        private SerializedProperty copyOnBuild;
        #endregion

        #region Properties
        /// <summary>
        /// Name for the serialized settings
        /// </summary>
        public override string SerializedName { get; } = BuildSettings.STANDALONE_SETTINGS_NAME;

        #if UNITY_2020_2_OR_NEWER
        private int ArchitectureIndex
        {
            get
            {
                switch (OSXUserBuildSettings.architecture)
                {
                    case MacOSArchitecture.x64:
                        return 0;
                    case MacOSArchitecture.ARM64:
                        return 1;
                    case MacOSArchitecture.x64ARM64:
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
                        OSXUserBuildSettings.architecture = MacOSArchitecture.x64;
                        break;

                    case 1:
                        OSXUserBuildSettings.architecture = MacOSArchitecture.ARM64;
                        break;

                    case 2:
                        OSXUserBuildSettings.architecture = MacOSArchitecture.x64ARM64;
                        break;

                    default:
                        OSXUserBuildSettings.architecture = MacOSArchitecture.x64;
                        break;
                }
            }
        }
        #endif
        #endregion

        #region Methods
        /// <summary>
        /// Makes sure the object is correctly initialized
        /// </summary>
        public override void Init()
        {
            base.Init();
            this.copyOnBuild = BuildToolWindow.Window.SerializedSettings.FindProperty(BuildToolSettings.COPY_ON_BUILD_NAME);
        }

        public override void OnGUI()
        {
            EditorUserBuildSettings.enableHeadlessMode = EditorGUILayout.Toggle(serverBuildLabel, EditorUserBuildSettings.enableHeadlessMode);
            base.OnGUI();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Windows Only", EditorStyles.boldLabel);
            WindowsUserBuildSettings.copyPDBFiles   = EditorGUILayout.Toggle(copyPDBLabel,          WindowsUserBuildSettings.copyPDBFiles);
            WindowsUserBuildSettings.createSolution = EditorGUILayout.Toggle(createVSSolutionLabel, WindowsUserBuildSettings.createSolution);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("MacOS Only", EditorStyles.boldLabel);
            OSXUserBuildSettings.createXcodeProject = EditorGUILayout.Toggle(createXcodeProjectLabel, OSXUserBuildSettings.createXcodeProject);
            #if UNITY_2020_2_OR_NEWER
            this.ArchitectureIndex = EditorGUILayout.Popup(architectureLabel, this.ArchitectureIndex, architectureValues);
            #endif

            EditorGUILayout.Space();
            using (VerticalScope.Enter())
            {
                //Copy files and folders panel header
                EditorGUILayout.LabelField(copyLabel, EditorStyles.boldLabel);
                using (HorizontalScope.Enter())
                {
                    //Add file browser
                    if (GUILayout.Button("Add file...", EditorStyles.miniButton, addButtonOptions))
                    {
                        //Get relative path
                        string relative = BuildToolUtils.OpenProjectFilePanel("Select file to copy on build");
                        //If a valid file is selected, add it
                        if (!string.IsNullOrEmpty(relative) && !this.copyOnBuild.Children().Any(p => p.Contains(BuildItem.PATH_NAME, relative)))
                        {
                            //Increment size and set at the end
                            this.copyOnBuild.arraySize++;
                            SerializedProperty item = this.copyOnBuild.GetArrayElementAtIndex(this.copyOnBuild.arraySize - 1);
                            item.NextVisible(true); //Path
                            item.stringValue = relative;
                        }
                    }

                    GUILayout.Space(20f);
                    if (GUILayout.Button("Add folder...", EditorStyles.miniButton, addButtonOptions))
                    {
                        //Get relative path
                        string relative = BuildToolUtils.OpenProjectFolderPanel("Select folder to copy on build");
                        //If a valid folder is selected, add it
                        if (!string.IsNullOrEmpty(relative) && !this.copyOnBuild.Children().Any(p => p.Contains(BuildItem.PATH_NAME, relative)))
                        {
                            //Increment size and set at the end
                            this.copyOnBuild.arraySize++;
                            SerializedProperty item = this.copyOnBuild.GetArrayElementAtIndex(this.copyOnBuild.arraySize - 1);
                            item.NextVisible(true); //Path
                            item.stringValue = relative;
                        }
                    }
                }

                EditorGUILayout.Space(5f);
                int index = 0, toDelete = -1;
                //List all folders and files to copy
                foreach (SerializedProperty toCopy in this.copyOnBuild)
                {
                    using (HorizontalScope.Enter())
                    {
                        //Delete entry button
                        GUI.backgroundColor = StylesUtils.Red;
                        if (GUILayout.Button(deleteEntry, StylesUtils.DeleteButtonStyle, deleteButtonOptions))
                        {
                            //Store index to delete it later (else it breaks the enumeration
                            toDelete = index;
                        }

                        GUI.backgroundColor = Color.white;
                        //Entry to copy label
                        toCopy.NextVisible(true); //Path
                        string path = toCopy.stringValue;
                        //Make sure the path isn't too long to be displayed
                        if (path.Length > MAX_PATH_LENGTH)
                        {
                            path = path.Substring(0, MAX_PATH_LENGTH - 3) + "...";
                        }

                        EditorGUILayout.LabelField(new GUIContent(path, toCopy.stringValue), GUILayout.Width(140f));
                        toCopy.NextVisible(true); //Location
                        toCopy.intValue = (int)(BuildItem.CopyLocation)EditorGUILayout.EnumPopup((BuildItem.CopyLocation)toCopy.intValue, copyPopupOptions);
                    }

                    index++;
                }

                //If an entry has been marked to delete, delete it now
                if (toDelete != -1)
                {
                    this.copyOnBuild.DeleteArrayElementAtIndex(toDelete);
                }
            }
        }
        #endregion
    }
}
