using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Ionic.Zip;
using UnityEditor;
using UnityEngine;

namespace BuildTool
{
    /// <summary>
    /// The BuildTarget enum under flag form, intended to pass multiple targets to build at once
    /// </summary>
    [Flags, SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum BuildTargetFlags
    {
        //Gotta love binary literals
        None     = 0b00000000000,
        Win64    = 0b00000000001,
        OSX      = 0b00000000010,
        Linux64  = 0b00000000100,
        Android  = 0b00000001000,
        iOS      = 0b00000010000,
        WebGL    = 0b00000100000,
        WinStore = 0b00001000000,
        XboxOne  = 0b00010000000,
        PS4      = 0b00100000000,
        Switch   = 0b01000000000,
        tvOS     = 0b10000000000
    }

    /// <summary>
    /// Utility methods for the BuildTool
    /// </summary>
    public static class BuildToolUtils
    {
        #region Constants
        /// <summary>
        /// Dictionary giving a nice name for BuildTargets
        /// </summary>
        private static readonly Dictionary<BuildTarget, string> targetNames = new Dictionary<BuildTarget, string>(11)
        {
            [BuildTarget.StandaloneWindows64] = "Win64",
            [BuildTarget.StandaloneOSX]       = "OSX",
            [BuildTarget.StandaloneLinux64]   = "Linux64",
            [BuildTarget.Android]             = "Android",
            [BuildTarget.iOS]                 = "iOS",
            [BuildTarget.WebGL]               = "WebGL",
            [BuildTarget.WSAPlayer]           = "WinStore",
            [BuildTarget.XboxOne]             = "XboxOne",
            [BuildTarget.PS4]                 = "PS4",
            [BuildTarget.Switch]              = "Switch",
            [BuildTarget.tvOS]                = "tvOS"
        };
        /// <summary>
        /// Array containing BuildTargetFlags and their equivalent BuildTarget
        /// </summary>
        private static readonly (BuildTargetFlags, BuildTarget)[] targets =
        {
            (BuildTargetFlags.None,     BuildTarget.NoTarget),
            (BuildTargetFlags.Win64,    BuildTarget.StandaloneWindows64),
            (BuildTargetFlags.OSX,      BuildTarget.StandaloneOSX),
            (BuildTargetFlags.Linux64,  BuildTarget.StandaloneLinux64),
            (BuildTargetFlags.Android,  BuildTarget.Android),
            (BuildTargetFlags.iOS,      BuildTarget.iOS),
            (BuildTargetFlags.WebGL,    BuildTarget.WebGL),
            (BuildTargetFlags.WinStore, BuildTarget.WSAPlayer),
            (BuildTargetFlags.XboxOne,  BuildTarget.XboxOne),
            (BuildTargetFlags.PS4,      BuildTarget.PS4),
            (BuildTargetFlags.Switch,   BuildTarget.Switch),
            (BuildTargetFlags.tvOS,     BuildTarget.tvOS)
        };
        /// <summary>
        /// The directory separator char as a string
        /// </summary>
        private static readonly string separatorString = Path.DirectorySeparatorChar.ToString();
        /// <summary>
        /// Red colour used throughout the UI
        /// </summary>
        public static readonly Color Red = new Color(0.7f, 0.1f, 0.1f);
        /// <summary>
        /// Green colour used throughout the UI
        /// </summary>
        public static readonly Color Green = new Color(0.1f, 0.6f, 0.1f);
        #endregion

        #region Static properties
        /// <summary>
        /// Full path of the project folder
        /// </summary>
        public static string ProjectFolderPath { get; } = Directory.GetParent(Application.dataPath).FullName;

        private static GUIStyle backgroundStyle;
        /// <summary>
        /// The background style of the selection UI
        /// </summary>
        public static GUIStyle BackgroundStyle
        {
            get
            {
                //Create the style if it hasn't been yet or the texture has unloaded
                if (backgroundStyle is null || !backgroundStyle.normal.background)
                {
                    //Create a transparent black texture to apply shade
                    Texture2D tex = new Texture2D(1, 1);
                    tex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.1f));
                    tex.Apply();
                    //Create style
                    backgroundStyle = new GUIStyle(GUI.skin.box) { normal = { background = tex } };
                }
                //Return the style
                return backgroundStyle;
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Gets the nice name for a given BuildTarget
        /// </summary>
        /// <param name="target">BuildTarget to get the name for</param>
        /// <returns>The nice name of this given BuildTarget</returns>
        public static string GetBuildTargetName(BuildTarget target) => targetNames[target];

        /// <summary>
        /// Gets all the BuildTargets out of a flag object
        /// </summary>
        /// <param name="flags">BuildTarget flags</param>
        /// <returns>An Enumerable of the BuildTargets held in the flag</returns>
        public static IEnumerable<BuildTarget> GetTargets(BuildTargetFlags flags)
        {
            //If there are no set flags, return
            if (flags == BuildTargetFlags.None) { yield break; }

            //Loop through all targets
            foreach ((BuildTargetFlags flag, BuildTarget target) in targets)
            {
                //Check for the presence of the flag
                if ((flags & flag) == flag)
                {
                    //Yield the target
                    yield return target;
                }
            }
        }

        /// <summary>
        /// Gets the relative path to a given folder
        /// </summary>
        /// <param name="path">Path to get the relative for</param>
        /// <param name="folder">Folder to get the relative from</param>
        /// <returns>The relative path from the specified folder</returns>
        public static string GetRelativePath(string path, string folder)
        {
            //If the path or folder is empty, return an empty string
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(folder)) { return string.Empty; }

            //Ensure the folder ends with a slash
            if (!folder.EndsWith(separatorString))
            {
                folder += separatorString;
            }
            //Create relative path
            return Uri.UnescapeDataString(new Uri(folder).MakeRelativeUri(new Uri(path)).ToString());
        }

        /// <summary>
        /// Checks if a SerializedProperty contains a given value
        /// </summary>
        /// <param name="property">Property to check</param>
        /// <param name="value">Value to find</param>
        /// <returns>True if the value is contained in the SerializedProperty, false otherwise</returns>
        public static bool PropertyContains(SerializedProperty property, string value)
        {
            //Loop through the property
            foreach (SerializedProperty prop in property)
            {
                //If a value equals the search, return true
                if (prop.stringValue == value) { return true; }
            }
            //If not return false
            return false;
        }

        /// <summary>
        /// Opens a file selection panel within the project folder and returns the relative path to the file selected
        /// </summary>
        /// <param name="title">Title for the file selection panel</param>
        /// <param name="extension">Filter extension for files</param>
        /// <returns>The relative path from the project root to the selected file</returns>
        public static string OpenProjectFilePanel(string title, string extension = "") => GetRelativePath(EditorUtility.OpenFilePanel(title, ProjectFolderPath, extension), ProjectFolderPath);

        /// <summary>
        /// Opens a folder selection panel within the project folder and returns the relative path to the folder selected
        /// </summary>
        /// <param name="title">Title for the folder selection panel</param>
        /// <param name="defaultName">Default selected folder name</param>
        /// <returns>The relative path from the project root to the selected folder</returns>
        public static string OpenProjectFolderPanel(string title, string defaultName = "") => GetRelativePath(EditorUtility.OpenFolderPanel(title, ProjectFolderPath, defaultName), ProjectFolderPath);

        /// <summary>
        /// Gets the relative path to the build's application data folder
        /// </summary>
        /// <param name="target">The BuildTarget to get the path for</param>
        /// <returns>The relative path to the application data folder, or an empty string if the platform isn't supported</returns>
        public static string GetAppDataPath(BuildTarget target)
        {
            switch (target)
            {
                //%appname%_Data/ for Windows and Linux
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.StandaloneLinux64:
                    return PlayerSettings.productName + "_Data";

                //%appname%.app/Contents/ for OSX
                case BuildTarget.StandaloneOSX:
                    return PlayerSettings.productName + ".app/Contents";

                //TODO: Probably test and figure out where to save for these
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Copies a file from a source path to a destination path asynchronously
        /// </summary>
        /// <param name="source">Source path to the file to copy</param>
        /// <param name="destination">Destination path to copy to</param>
        /// <returns>The awaitable copy I/O task</returns>
        public static async Task CopyFileAsync(string source, string destination)
        {
            //Open in and out streams
            using (FileStream sourceStream = File.OpenRead(source))
            using (FileStream destinationStream = File.OpenWrite(destination))
            {
                //Await copy
                await sourceStream.CopyToAsync(destinationStream);
            }
        }

        /// <summary>
        /// Copies a folder and all it's contents recursively from a source path to a destination path asynchronously
        /// </summary>
        /// <param name="source">Source path to copy from</param>
        /// <param name="destination">Destination path to copy to</param>
        /// <returns>The awaitable copy I/O task</returns>
        public static async Task CopyFolderAsync(string source, string destination)
        {
            //Loop through all the subdirectories of the source dir
            foreach (DirectoryInfo dir in new DirectoryInfo(source).EnumerateDirectories("*", SearchOption.AllDirectories))
            {
                //Get the output dir
                string outputDir = dir.FullName.Replace(source, destination);
                //Make sure the directory is created
                Directory.CreateDirectory(outputDir);

                //Loop through all the files of the directory
                foreach (FileInfo file in dir.EnumerateFiles())
                {
                    //Open in and out streams
                    using (FileStream sourceStream = file.OpenRead())
                    using (FileStream destinationStream = File.OpenWrite(Path.Combine(outputDir, file.Name)))
                    {
                        //Await copy
                        await sourceStream.CopyToAsync(destinationStream);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a new zip file asynchronously at the given path, and adds the given source directory to it
        /// </summary>
        /// <param name="path">Path to create the zip file at</param>
        /// <param name="source">Source directory to add to the zip file</param>
        /// <returns>The task associated to this zip file creation</returns>
        public static async Task CreateZipAsync(string path, string source) => await Task.Run(() => CreateZip(path, source));

        /// <summary>
        /// Creates a new zip file at the given path, and adds the given source directory to it
        /// </summary>
        /// <param name="path">Path to create the zip file at</param>
        /// <param name="source">Source directory to add to the zip file</param>
        public static void CreateZip(string path, string source)
        {
            //Create zip file
            using (ZipFile zip = new ZipFile(path))
            {
                //Add directory, then save the file
                zip.AddDirectory(source);
                zip.Save();
            }
        }
        #endregion
    }
}
