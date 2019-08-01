using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

#pragma warning disable IDE0044 //Add readonly modifier
#pragma warning disable CS0649  //Field never assigned

namespace BuildTool
{
    /// <summary>
    /// Build tool settings object
    /// </summary>
    public class BuildToolSettings : ScriptableObject
    {
        #region Constants
        /// <summary>
        /// Settings file name
        /// </summary>
        private const string filePath = "Assets/BuildTool/Editor/BuildToolSettings.asset";
        /// <summary>
        /// Name of the BuildRepository SerializedProperty
        /// </summary>
        public const string BUILD_REPOSITORY_NAME  = nameof(buildRepository);
        /// <summary>
        /// Name of the VersionURL SerializedProperty
        /// </summary>
        public const string VERSION_URL_NAME       = nameof(versionURL);
        /// <summary>
        /// Name of the DevelopmentBuild SerializedProperty
        /// </summary>
        public const string DEVELOPMENT_BUILD_NAME = nameof(developmentBuild);
        /// <summary>
        /// Name of the OutputFolder SerializedProperty
        /// </summary>
        public const string OUTPUT_FOLDER_NAME     = nameof(outputFolder);
        /// <summary>
        /// Name of the TargetFlags SerializedProperty
        /// </summary>
        public const string TARGET_FLAGS_NAME      = nameof(targetFlags);
        /// <summary>
        /// Name of the CopyOnBuild SerializedProperty
        /// </summary>
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
        private List<string> copyOnBuild = new List<string>();
        /// <summary>
        /// List of files and folders to copy on build
        /// </summary>
        public List<string> CopyOnBuild => this.copyOnBuild;
        #endregion

        #region Static methods
        /// <summary>
        /// Loads the BuildToolSettings asset into memory, else creates it
        /// </summary>
        public static BuildToolSettings Load()
        {
            //Load the asset file. We're not using the generic method here because the as cast will produce a null instead of an error
            BuildToolSettings settings = AssetDatabase.LoadAssetAtPath(filePath, typeof(BuildToolSettings)) as BuildToolSettings;

            //If the settings object is null, create a new one
            if (!settings)
            {
                settings = CreateInstance<BuildToolSettings>();
                Directory.GetParent(filePath).Create();
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
