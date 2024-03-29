﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using BuildTool.Extensions;
#if !DEBUG
using UnityEditor;
using UnityEngine;
#endif
using CompressionLevel = System.IO.Compression.CompressionLevel;

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
        Win32    = 0b00000000001,
        Win64    = 0b00000000010,
        OSX      = 0b00000000100,
        Linux64  = 0b00000001000,
      //Android  = 0b00000010000,  Not supported yet
        iOS      = 0b00000100000,
        WebGL    = 0b00001000000,
      //XboxOne  = 0b00010000000,  Not supported yet
      //PS4      = 0b00100000000,  Not supported yet
      //Switch   = 0b01000000000,  Not supported yet
        tvOS     = 0b10000000000
    }

    /// <summary>
    /// Utility methods for the BuildTool
    /// </summary>
    public static class BuildToolUtils
    {
        #region Constants
        #if !DEBUG
        /// <summary>
        /// Dictionary giving a nice name for BuildTargets
        /// </summary>
        private static readonly Dictionary<BuildTarget, string> targetNames = new Dictionary<BuildTarget, string>(7)
        {
            [BuildTarget.StandaloneWindows]   = "Win32",
            [BuildTarget.StandaloneWindows64] = "Win64",
            [BuildTarget.StandaloneOSX]       = "OSX",
            [BuildTarget.StandaloneLinux64]   = "Linux64",
          //[BuildTarget.Android]             = "Android", Not supported yet
            [BuildTarget.iOS]                 = "iOS",
            [BuildTarget.WebGL]               = "WebGL",
          //[BuildTarget.XboxOne]             = "XboxOne", Not supported yet
          //[BuildTarget.PS4]                 = "PS4",     Not supported yet
          //[BuildTarget.Switch]              = "Switch",  Not supported yet
            [BuildTarget.tvOS]                = "tvOS"
        };
        /// <summary>
        /// Array containing BuildTargetFlags and their equivalent BuildTarget
        /// </summary>
        private static readonly (BuildTargetFlags, BuildTarget)[] targets =
        {
            (BuildTargetFlags.None,     BuildTarget.NoTarget),
            (BuildTargetFlags.Win32,    BuildTarget.StandaloneWindows),
            (BuildTargetFlags.Win64,    BuildTarget.StandaloneWindows64),
            (BuildTargetFlags.OSX,      BuildTarget.StandaloneOSX),
            (BuildTargetFlags.Linux64,  BuildTarget.StandaloneLinux64),
          //(BuildTargetFlags.Android,  BuildTarget.Android), Not supported yet
            (BuildTargetFlags.iOS,      BuildTarget.iOS),
            (BuildTargetFlags.WebGL,    BuildTarget.WebGL),
          //(BuildTargetFlags.XboxOne,  BuildTarget.XboxOne), Not supported yet
          //(BuildTargetFlags.PS4,      BuildTarget.PS4),     Not supported yet
          //(BuildTargetFlags.Switch,   BuildTarget.Switch),  Not supported yet
            (BuildTargetFlags.tvOS,     BuildTarget.tvOS)
        };
        /// <summary>
        /// Red colour used throughout the UI
        /// </summary>
        public static readonly Color Red = new Color(0.7f, 0.1f, 0.1f);
        /// <summary>
        /// Green colour used throughout the UI
        /// </summary>
        public static readonly Color Green = new Color(0.1f, 0.6f, 0.1f);
        #endif
        /// <summary>
        /// The directory separator char as a string
        /// </summary>
        private static readonly string separatorString = Path.DirectorySeparatorChar.ToString();
        #endregion

        #region Static properties
        /// <summary>
        /// The path to the data directory
        /// </summary>
        #if DEBUG
        public static string DataPath { get; } = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        #else
        public static string DataPath => Application.dataPath;
        #endif

        /// <summary>
        /// Name of the Product being built
        /// </summary>
        #if DEBUG
        public static string ProductName { get; } = Assembly.GetExecutingAssembly().GetName().Name;
        #else
        public static string ProductName => PlayerSettings.productName;
        #endif

        /// <summary>
        /// Full path of the project folder
        /// </summary>
        public static string ProjectFolderPath { get; } = Directory.GetParent(DataPath).FullName;

        #if !DEBUG
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
        #endif
        #endregion

        #region Methods
        #if !DEBUG
        /// <summary>
        /// Gets the nice name for a given BuildTarget
        /// </summary>
        /// <param name="target">BuildTarget to get the name for</param>
        /// <exception cref="NotSupportedException">The BuildTarget is not supported by the tool</exception>
        /// <returns>The nice name of this given BuildTarget</returns>
        public static string GetBuildTargetName(BuildTarget target)
        {
            //Try and get the name
            if (!targetNames.TryGetValue(target, out string name))
            {
                //If it fails, throw a NotSupportedException
                throw new NotSupportedException(target + " build target is not supported by the UnityBuildTool");
            }
            return name;
        }

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
                if ((flags & flag) != 0)
                {
                    //Yield the target
                    yield return target;
                }
            }
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
        /// <param name="productName">Name of the product to look for, if omitted uses the default one</param>
        /// <returns>The relative path to the application data folder, or an empty string if the platform isn't supported</returns>
        public static string GetAppDataPath(BuildTarget target, string productName = null)
        {
            //Use optional product name if necessary
            if (string.IsNullOrEmpty(productName)) { productName = ProductName; }
            switch (target)
            {
                //%appname%_Data/ on Windows and Linux
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.StandaloneLinux64:
                    return productName + "_Data";

                //%appname%.app/Contents/ on OSX
                case BuildTarget.StandaloneOSX:
                    return Path.Combine(productName + ".app", "Contents");

                //%appname%/Data/ on WSA, iOS, and tvOS
                case BuildTarget.WSAPlayer:
                case BuildTarget.iOS:
                case BuildTarget.tvOS:
                    return Path.Combine(productName, "Data");

                //%appname%/Build on WebGL... I think?
                case BuildTarget.WebGL:
                    return Path.Combine(productName, "Build");

                //TODO: Probably test and figure out where to save for other targets
                default:
                    return string.Empty;
            }
        }
        #endif

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
        /// Copies a file from a source path to a destination path asynchronously
        /// </summary>
        /// <param name="source">Source path to the file to copy</param>
        /// <param name="destination">Destination path to copy to</param>
        /// <returns>The awaitable copy I/O task</returns>
        public static async Task CopyFileAsync(string source, string destination)
        {
            //Check for preconditions
            if (string.IsNullOrEmpty(source)) { throw new ArgumentNullException(nameof(source), "Source file name cannot be null or empty"); }
            if (string.IsNullOrEmpty(destination)) { throw new ArgumentNullException(nameof(destination), "Destination file name cannot be null or empty"); }
            source = Path.GetFullPath(source); //Ensure we don't have any /./ or /../ in our path
            if (!File.Exists(source)) { throw new FileNotFoundException($"The file {source} could not be found"); }

            //Make sure the destination directory exists
            DirectoryInfo parentDir = Directory.GetParent(destination);
            if (!parentDir.Exists)
            {
                parentDir.Create();
            }

            //Open in and out streams
            using (FileStream sourceStream = File.OpenRead(source))
            using (FileStream destinationStream = File.Create(destination))
            {
                //Await copy
                await sourceStream.CopyToAsync(destinationStream).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Copies a folder and all it's contents recursively from a source path to a destination path asynchronously
        /// </summary>
        /// <param name="source">Source path to copy from</param>
        /// <param name="destination">Destination path to copy to</param>
        /// <exception cref="ArgumentNullException">If the source or destination directories are null</exception>
        /// <exception cref="DirectoryNotFoundException">If the source directory cannot be found</exception>
        /// <returns>The awaitable copy I/O task</returns>
        public static async Task CopyFolderAsync(string source, string destination)
        {
            //Check for preconditions
            if (string.IsNullOrEmpty(source)) { throw new ArgumentNullException(nameof(source), "Source directory name cannot be null or empty"); }
            if (string.IsNullOrEmpty(destination)) { throw new ArgumentNullException(nameof(destination), "Destination folder name cannot be null or empty"); }
            source = Path.GetFullPath(source); //Ensure we don't have any /./ or /../ in our path
            if (!Directory.Exists(source)) { throw new DirectoryNotFoundException($"The directory {source} could not be found"); }

            //Loop through all the subdirectories of the source dir
            DirectoryInfo sourceDir = new DirectoryInfo(source);
            foreach (DirectoryInfo dir in sourceDir.Yield().Concat(sourceDir.EnumerateDirectories("*", SearchOption.AllDirectories)))
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
                    using (FileStream destinationStream = File.Create(Path.Combine(outputDir, file.Name)))
                    {
                        //Await copy
                        await sourceStream.CopyToAsync(destinationStream).ConfigureAwait(false);
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
        public static async Task CreateZipAsync(string path, string source)
        {
            //Check for preconditions
            if (string.IsNullOrEmpty(source)) { throw new ArgumentNullException(nameof(source), "Source directory name cannot be null or empty"); }
            if (string.IsNullOrEmpty(path)) { throw new ArgumentNullException(nameof(path), "Destination zip file name cannot be null or empty"); }
            source = Path.GetFullPath(source); //Ensure we don't have any /./ or /../ in our path
            if (!Directory.Exists(source)) { throw new DirectoryNotFoundException($"The directory {source} could not be found"); }

            //Create archive
            using (ZipArchive archive = new ZipArchive(File.Create(path), ZipArchiveMode.Create, false))
            {
                source = Path.GetFullPath(source);
                foreach (FileInfo file in new DirectoryInfo(source).EnumerateFiles("*", SearchOption.AllDirectories))
                {
                    using (Stream entryStream = archive.CreateEntry(GetRelativePath(file.FullName, source), CompressionLevel.Optimal).Open())
                    using (FileStream fileStream = file.OpenRead())
                    {
                        await fileStream.CopyToAsync(entryStream).ConfigureAwait(false);
                    }
                }
            }
        }
        #endregion
    }
}
