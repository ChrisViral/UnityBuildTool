﻿using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

//ReSharper disable UnusedType.Local

namespace UnityBuildTool
{
    /// <summary>
    /// Build tool settings object
    /// </summary>
    public sealed class BuildToolSettings : ScriptableObject
    {
        /// <summary>
        /// Custom Inspector for this type of asset
        /// </summary>
        [CustomEditor(typeof(BuildToolSettings))]
        private class BuildToolSettingsEditor : Editor
        {
            #region Fields
            private SerializedProperty buildRepository;
            private SerializedProperty useWebService;
            private SerializedProperty versionURL;
            private SerializedProperty developmentBuild;
            private SerializedProperty publishRelease;
            private SerializedProperty outputFolder;
            private SerializedProperty targetFlags;
            private SerializedProperty copyOnBuild;
            #endregion

            #region Methods
            private void OnEnable()
            {
                this.buildRepository  = this.serializedObject.FindProperty(BUILD_REPOSITORY_NAME);
                this.useWebService    = this.serializedObject.FindProperty(USE_WEB_SERVICE_NAME);
                this.versionURL       = this.serializedObject.FindProperty(VERSION_URL_NAME);
                this.developmentBuild = this.serializedObject.FindProperty(DEVELOPMENT_BUILD_NAME);
                this.publishRelease   = this.serializedObject.FindProperty(PUBLISH_RELEASE_NAME);
                this.outputFolder     = this.serializedObject.FindProperty(OUTPUT_FOLDER_NAME);
                this.targetFlags      = this.serializedObject.FindProperty(TARGET_FLAGS_NAME);
                this.copyOnBuild      = this.serializedObject.FindProperty(COPY_ON_BUILD_NAME);
            }

            /// <summary>
            /// Generate the inspector GUI
            /// </summary>
            public override void OnInspectorGUI()
            {
                //Normal property fields for everyone
                EditorGUILayout.PropertyField(this.buildRepository);
                EditorGUILayout.PropertyField(this.useWebService);
                EditorGUILayout.PropertyField(this.versionURL);
                EditorGUILayout.PropertyField(this.developmentBuild);
                EditorGUILayout.PropertyField(this.publishRelease);
                EditorGUILayout.PropertyField(this.outputFolder);
                //Except for the flags on
                this.targetFlags.intValue = (int)(BuildTargetFlags)EditorGUILayout.EnumFlagsField("Target Flags", (BuildTargetFlags)this.targetFlags.intValue);
                //And back to normal
                EditorGUILayout.PropertyField(this.copyOnBuild, true);

                if (this.serializedObject.hasModifiedProperties)
                {
                    //Apply everything
                    this.serializedObject.ApplyModifiedProperties();
                }
            }
            #endregion
        }

        #region Constants
        /// <summary>Settings file name</summary>
        private const string filePath = "Assets/UnityBuildTool/Editor/BuildToolSettings.asset";
        /// <summary>Name of the BuildRepository SerializedProperty</summary>
        public const string BUILD_REPOSITORY_NAME  = nameof(buildRepository);
        /// <summary>Name of the UseWebService SerializedProperty</summary>
        public const string USE_WEB_SERVICE_NAME   = nameof(useWebService);
        /// <summary>Name of the VersionURL SerializedProperty</summary>
        public const string VERSION_URL_NAME       = nameof(versionURL);
        /// <summary>Name of the DevelopmentBuild SerializedProperty</summary>
        public const string DEVELOPMENT_BUILD_NAME = nameof(developmentBuild);
        /// <summary>Name of the PublishRelease SerializedProperty</summary>
        public const string PUBLISH_RELEASE_NAME   = nameof(publishRelease);
        /// <summary>Name of the OutputFolder SerializedProperty</summary>
        public const string OUTPUT_FOLDER_NAME     = nameof(outputFolder);
        /// <summary>Name of the TargetFlags SerializedProperty</summary>
        public const string TARGET_FLAGS_NAME      = nameof(targetFlags);
        /// <summary>Name of the CopyOnBuild SerializedProperty</summary>
        public const string COPY_ON_BUILD_NAME     = nameof(copyOnBuild);
        #endregion

        #region Properties
        [SerializeField]
        private string buildRepository = string.Empty;
        /// <summary>
        /// The last used build repository
        /// </summary>
        public string BuildRepository => this.buildRepository;

        [SerializeField]
        private bool useWebService;
        /// <summary>
        /// If a BuildVersion web service is used
        /// </summary>
        public bool UseWebService => this.useWebService;

        [SerializeField]
        private string versionURL = string.Empty;
        /// <summary>
        /// The URL of the hosted Version API
        /// </summary>
        public string VersionURL => this.versionURL;

        [SerializeField]
        private bool developmentBuild;
        /// <summary>
        /// If the build to do must be a development build or not
        /// </summary>
        public bool DevelopmentBuild => this.developmentBuild;

        [SerializeField]
        private bool publishRelease = true;
        /// <summary>
        /// If the release should be published to GitHub or not
        /// </summary>
        public bool PublishRelease => this.publishRelease;

        [SerializeField]
        private string outputFolder = string.Empty;
        /// <summary>
        /// The local path of the build output directory
        /// </summary>
        public string OutputFolder => this.outputFolder;

        [SerializeField]
        private BuildTargetFlags targetFlags = BuildTargetFlags.None;
        /// <summary>
        /// Platform targets to build
        /// </summary>
        public BuildTargetFlags TargetFlags => this.targetFlags;

        [SerializeField]
        private List<BuildItem> copyOnBuild = new List<BuildItem>();
        /// <summary>
        /// List of files and folders to copy on build
        /// </summary>
        public List<BuildItem> CopyOnBuild => this.copyOnBuild;
        #endregion

        #region Static methods
        /// <summary>
        /// Loads the BuildToolSettings asset into memory, else creates it
        /// </summary>
        public static BuildToolSettings Load()
        {
            //Load the asset file. We're not using the generic method here because the as cast will produce a null instead of an error
            BuildToolSettings settings = AssetDatabase.LoadAssetAtPath<BuildToolSettings>(filePath);

            //If the settings object is null, create a new one
            if (!settings)
            {
                settings = CreateInstance<BuildToolSettings>();
                AssetDatabase.CreateAsset(settings, filePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            //Return instance
            return settings;
        }
        #endregion
    }
}
