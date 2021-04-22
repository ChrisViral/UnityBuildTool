using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Ionic.Zip;
using UnityBuildTool.Extensions;
using UnityEditor;
using UnityEngine;

namespace UnityBuildTool
{
    /// <summary>
    /// The BuildTarget enum under flag form, intended to pass multiple targets to build at once
    /// </summary>
    [Flags]
    public enum BuildTargetFlags
    {
        //Gotta love binary literals
        None     = 0b0000,
        Win32    = 0b0001,
        Win64    = 0b0010,
        OSX      = 0b0100,
        Linux64  = 0b1000
    }

    /// <summary>
    /// Utility methods for the BuildTool
    /// </summary>
    public static class BuildToolUtils
    {
        #region Constants
        /// <summary>Dictionary giving a nice name for BuildTargets</summary>
        private static readonly Dictionary<BuildTarget, string> targetNames = new Dictionary<BuildTarget, string>(4)
        {
            [BuildTarget.StandaloneWindows]   = "Win32",
            [BuildTarget.StandaloneWindows64] = "Win64",
            [BuildTarget.StandaloneOSX]       = "OSX",
            [BuildTarget.StandaloneLinux64]   = "Linux64",
        };
        /// <summary>Array containing BuildTargetFlags and their equivalent BuildTarget</summary>
        private static readonly (BuildTargetFlags, BuildTarget)[] targets =
        {
            (BuildTargetFlags.None,     BuildTarget.NoTarget),
            (BuildTargetFlags.Win32,    BuildTarget.StandaloneWindows),
            (BuildTargetFlags.Win64,    BuildTarget.StandaloneWindows64),
            (BuildTargetFlags.OSX,      BuildTarget.StandaloneOSX),
            (BuildTargetFlags.Linux64,  BuildTarget.StandaloneLinux64),
        };
        /// <summary>The directory separator char as a string</summary>
        private static readonly string separatorString = Path.DirectorySeparatorChar.ToString();
        #endregion

        #region Static properties
        /// <summary>
        /// The path to the data directory
        /// </summary>
        public static string DataPath => Application.dataPath;

        /// <summary>
        /// Name of the Product being built
        /// </summary>
        public static string ProductName => PlayerSettings.productName;

        /// <summary>
        /// Full path of the project folder
        /// </summary>
        public static string ProjectFolderPath { get; } = Directory.GetParent(DataPath).FullName;
        #endregion

        #region Methods
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
            if (flags == BuildTargetFlags.None) yield break;

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
            if (string.IsNullOrEmpty(productName)) productName = ProductName;

            switch (target)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.StandaloneLinux64:
                    return productName + "_Data";

                case BuildTarget.StandaloneOSX:
                    return Path.Combine(productName + ".app", "Contents");

                default:
                    return string.Empty;
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
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(folder)) return string.Empty;

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
            if (string.IsNullOrEmpty(source)) throw new ArgumentNullException(nameof(source), "Source file name cannot be null or empty");
            if (string.IsNullOrEmpty(destination)) throw new ArgumentNullException(nameof(destination), "Destination file name cannot be null or empty");

            source = Path.GetFullPath(source); //Ensure we don't have any /./ or /../ in our path
            if (!File.Exists(source)) throw new FileNotFoundException($"The file {source} could not be found");

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
            if (string.IsNullOrEmpty(source)) throw new ArgumentNullException(nameof(source), "Source directory name cannot be null or empty");
            if (string.IsNullOrEmpty(destination)) throw new ArgumentNullException(nameof(destination), "Destination folder name cannot be null or empty");

            source = Path.GetFullPath(source); //Ensure we don't have any /./ or /../ in our path
            if (!Directory.Exists(source)) throw new DirectoryNotFoundException($"The directory {source} could not be found");

            //Loop through all the subdirectories of the source dir
            DirectoryInfo sourceDir = new DirectoryInfo(source);
            foreach (DirectoryInfo dir in sourceDir.EnumerateAllDirectories())
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
            if (string.IsNullOrEmpty(source)) throw new ArgumentNullException(nameof(source), "Source directory name cannot be null or empty");
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path), "Destination zip file name cannot be null or empty");

            source = Path.GetFullPath(source); //Ensure we don't have any /./ or /../ in our path
            if (!Directory.Exists(source)) throw new DirectoryNotFoundException($"The directory {source} could not be found");

            //Create archive
            using (ZipFile zip = new ZipFile(path))
            {
                //ReSharper disable once AccessToDisposedClosure
                await Task.Run(() => zip.AddDirectory(source)).ConfigureAwait(false);
                await Task.Run(zip.Save).ConfigureAwait(false);
            }
        }
        #endregion
    }
}
