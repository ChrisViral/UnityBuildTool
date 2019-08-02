using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BuildTool.Extensions;
using BuildTool.Json;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace BuildTool
{
    /// <summary>
    /// Game build version object
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class BuildVersion
    {
        /// <summary>
        /// Indicates what action should be done on the version number on build
        /// </summary>
        public enum VersionBump
        {
            Revision,
            Build,
            Minor,
            Major
        }

        #region Constants
        /// <summary>
        /// File extension of the build files
        /// </summary>
        public const string EXTENSION = ".build";
        /// <summary>
        /// The Build time format string
        /// </summary>
        public const string TIME_FORMAT = "dd/MM/yyyy-HH:mm:ss";
        /// <summary>
        /// The regex pattern to parse the build file
        /// </summary>
        public const string PATTERN = @"v(\d+\.\d+\.\d+\.\d+)\|([\w-]+)@(\d{2}/\d{2}/\d{4}-\d{2}:\d{2}:\d{2})UTC";
        /// <summary>
        /// Path to the version file on the disk
        /// </summary>
        public static readonly string FilePath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, PlayerSettings.productName.ToLowerInvariant() + EXTENSION);
        #endregion

        #region Fields
        //Version fields
        private int major, minor, build, revision;
        #endregion

        #region Properties
        /// <summary>
        /// Version number of the build
        /// </summary>
        [JsonProperty("version", Required = Required.Always, Order = 0), JsonConverter(typeof(VersionConverter))]
        public Version Version
        {
            get => new Version(this.major, this.minor, this.build, this.revision);
            set
            {
                //Check for null
                if (value is null) { throw new ArgumentNullException(nameof(value), "Cannot set null version"); }

                //Set the individual values
                this.major = value.Major;
                this.minor = value.Minor;
                this.build = value.Build;
                this.revision = value.Revision;
            }
        }

        /// <summary>
        /// The string version of the current version
        /// </summary>
        public string VersionString => $"v{this.major}.{this.minor}.{this.build}.{this.revision}";

        /// <summary>
        /// Date and time of the build
        /// </summary>
        [JsonProperty("build_time", Required =  Required.Always, Order = 1), JsonConverter(typeof(VersionDateConverter))]
        public DateTime BuildTime { get; private set; }

        /// <summary>
        /// The date string of the UTC now time
        /// </summary>
        public string NowDateString => DateTime.UtcNow.ToString(TIME_FORMAT, CultureInfo.InvariantCulture);

        /// <summary>
        /// Author of the build
        /// </summary>
        [JsonProperty("author", Required =  Required.Always, Order = 2)]
        public string Author { get; private set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Prevents instantiation
        /// </summary>
        [JsonConstructor]
        private BuildVersion() { }
        #endregion

        #region Static methods
        /// <summary>
        /// Loads a BuildVersion from the file on the disk, if it fails, a new default version is created
        /// </summary>
        /// <returns>The loaded or created BuildVersion</returns>
        public static BuildVersion FromFile()
        {
            //Get file path
            BuildVersion build = new BuildVersion();

            //Check if file exists
            if (!File.Exists(FilePath)) { build.Log("Version file could not be found"); }
            else
            {
                try
                {
                    //Read version info from file
                    string[] data = Regex.Match(File.ReadAllText(FilePath, Encoding.ASCII).Trim(), PATTERN)
                                         .Groups.Cast<Group>()
                                         .Skip(1)  //First group is always the entire match
                                         .Select(g => g.Captures[0].Value)
                                         .ToArray();

                    //Parse version info
                    build.Version = new Version(data[0]);
                    build.Author = data[1];
                    build.BuildTime = DateTime.SpecifyKind(DateTime.ParseExact(data[2], TIME_FORMAT, CultureInfo.InvariantCulture), DateTimeKind.Utc);

                    //Log and return the created object
                    build.Log("Version object sucessfully loaded from file");
                    return build;
                }
                catch (Exception e)
                {
                    //Log exception
                    build.LogException(e);
                }
            }

            //Create new build and save to file
            build.Log("Creating new BuildVersion and file...");
            build.Version = new Version(0, 1, 0, 0);
            build.Author = "---";
            build.BuildTime = DateTime.UtcNow;
            build.SaveToFile();
            return build;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Gets the bumped up version string without actually applying it to the version object
        /// </summary>
        /// <param name="bump">The Version bump to apply</param>
        /// <returns>The Updated version string</returns>
        public string GetBumpedVersionString(VersionBump bump)
        {
            //Bump the version number accordingly
            switch (bump)
            {
                case VersionBump.Major:
                    return $"v{this.major + 1}.0.0.{this.revision + 1}";
                case VersionBump.Minor:
                    return $"v{this.major}.{this.minor + 1}.0.{this.revision + 1}";
                case VersionBump.Build:
                    return $"v{this.major}.{this.minor}.{this.build + 1}.{this.revision + 1}";
                default:
                    return $"v{this.major}.{this.minor}.{this.build}.{this.revision + 1}";
            }
        }

        /// <summary>
        /// Gets the game's version after the next build, with the given version bump information
        /// </summary>
        /// <param name="bump">How to bump the version over this build</param>
        /// <param name="author">The build's author</param>
        /// <param name="uploadURL">The URL to upload the version to if desired</param>
        /// <returns>The new version after the build</returns>
        public void Build(VersionBump bump, string author, string uploadURL = null)
        {
            //Bump the version number accordingly
            switch (bump)
            {
                case VersionBump.Build:
                    this.build++;
                    break;

                case VersionBump.Minor:
                    this.build = 0;
                    this.minor++;
                    break;

                case VersionBump.Major:
                    this.build = this.minor = 0;
                    this.major++;
                    break;
            }

            //Set the author, date, and bump the revision number
            this.Author = author;
            this.BuildTime = DateTime.UtcNow;
            this.revision++;

            //Save the new build to the disk
            SaveToFile();

            //If the version must be uploaded, do it now
            if (!string.IsNullOrEmpty(uploadURL))
            {
                JsonWebClient.PostJsonObject(uploadURL, this);
            }
        }

        /// <summary>
        /// Saves this BuildVersion to the disk
        /// </summary>
        public void SaveToFile() => File.WriteAllText(FilePath, ToString(), Encoding.ASCII);

        /// <summary>
        ///A string version of the build
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"{this.VersionString}|{this.Author}@{this.NowDateString}UTC";
        #endregion
    }
}
