using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using BuildTool.Extensions;
#if !DEBUG
using BuildTool.UI;
using UnityEditor;
#endif

#pragma warning disable IDE0051 //Remove unused private members

namespace BuildTool
{
    /// <summary>
    /// Game build version object
    /// </summary>
    [DataContract]
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

        /// <summary>
        /// Proxy struct to serialize a Version object correctly
        /// </summary>
        [DataContract]
        private readonly struct VersionProxy
        {
            #region Fields
            /// <summary>
            /// Major version number
            /// </summary>
            [DataMember(IsRequired = true, Order = 0)]
            public readonly int major;
            /// <summary>
            /// Minor version number
            /// </summary>
            [DataMember(IsRequired = true, Order = 1)]
            public readonly int minor;
            /// <summary>
            /// Build version number
            /// </summary>
            [DataMember(IsRequired = true, Order = 2)]
            public readonly int build;
            /// <summary>
            /// Revision version number
            /// </summary>
            [DataMember(IsRequired = true, Order = 3)]
            public readonly int revision;
            #endregion

            #region Constructors
            /// <summary>
            /// Creates a new VersionProxy with the given version numbers
            /// </summary>
            /// <param name="major">Major version number</param>
            /// <param name="minor">Minor version number</param>
            /// <param name="build">Build version number</param>
            /// <param name="revision">Revision version number</param>
            public VersionProxy(int major, int minor, int build, int revision)
            {
                this.major = major;
                this.minor = minor;
                this.build = build;
                this.revision = revision;
            }
            #endregion
        }

        #region Constants
        /// <summary>
        /// File extension of the build files
        /// </summary>
        public const string EXTENSION = ".build";
        /// <summary>
        /// The Build time format string
        /// </summary>
        public const string TIME_FORMAT = "dd/MM/yyyy-HH:mm:ssUTC";
        /// <summary>
        /// The regex pattern to parse the build file
        /// </summary>
        public const string PATTERN = @"v(\d+\.\d+\.\d+\.\d+)\|([\w-]+)@(\d{2}/\d{2}/\d{4}-\d{2}:\d{2}:\d{2}UTC)";
        #endregion

        #region Fields
        //Version fields
        private int major, minor, build, revision;
        #endregion

        #region Properties
        /// <summary>
        /// Version number of the build
        /// </summary>
        public Version Version
        {
            get => new Version(this.major, this.minor, this.build, this.revision);
            private set
            {
                //Check if null
                if (value is null) { throw new ArgumentNullException(nameof(value), "Assigned Version cannot be null"); }

                //Assign values individually
                this.major = value.Major;
                this.minor = value.Minor;
                this.build = value.Build;
                this.revision = value.Revision;
            }
        }

        /// <summary>
        /// Proxy version object to get and set the version in the Json correctly
        /// </summary>
        [DataMember(Name = "version", IsRequired = true, Order = 0)]
        private VersionProxy TempVersion
        {
            get => new VersionProxy(this.major, this.minor, this.build, this.revision);
            set
            {
                this.major = value.major;
                this.minor = value.minor;
                this.build = value.build;
                this.revision = value.revision;
            }
        }

        /// <summary>
        /// The string version of the current version
        /// </summary>
        public string VersionString => $"v{this.major}.{this.minor}.{this.build}.{this.revision}";

        /// <summary>
        /// Date and time of the build
        /// </summary>
        [DataMember(Name = "build_time", IsRequired = true, Order = 1)]
        public DateTime BuildTime { get; private set; }

        /// <summary>
        /// The date string of the UTC now time
        /// </summary>
        public string BuildDateString => this.BuildTime.ToString(TIME_FORMAT, CultureInfo.InvariantCulture);

        /// <summary>
        /// Author of the build
        /// </summary>
        [DataMember(Name = "author", IsRequired = true, Order = 2)]
        public string Author { get; private set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Default constructor, in-class use only
        /// </summary>
        private BuildVersion() { }

        /// <summary>
        /// Tests only instantiation
        /// </summary>
        /// <param name="major">Major version number</param>
        /// <param name="minor">Minor version number</param>
        /// <param name="build">Build version number</param>
        /// <param name="revision">Revision version number</param>
        /// <param name="author">Name of the author</param>
        /// <param name="time">DateTime of the build</param>
        internal BuildVersion(int major, int minor, int build, int revision, string author, DateTime time)
        {
            this.major = major;
            this.minor = minor;
            this.build = build;
            this.revision = revision;
            this.Author = author;
            this.BuildTime = time;
        }
        #endregion

        #region Static methods
        /// <summary>
        /// Loads a BuildVersion from the file on the disk, if it fails, a new default version is created
        /// </summary>
        /// <returns>The loaded or created BuildVersion</returns>
        public static BuildVersion FromFile()
        {

            //Get file path
            #if DEBUG
            string path = Path.Combine(BuildToolUtils.DataPath, BuildToolUtils.ProductName.ToLowerInvariant() + EXTENSION);
            #else
            string path = BuildToolWindow.BuildFilePath;
            #endif
            BuildVersion build = new BuildVersion();

            //Check if file exists
            if (!File.Exists(path)) { build.Log("Version file could not be found"); }
            else
            {
                try
                {
                    //Read version info from file
                    string[] data = Regex.Match(File.ReadAllText(path, Encoding.ASCII), PATTERN)
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

            #if !DEBUG
            //Set the version in Unity (without the v)
            PlayerSettings.bundleVersion = $"{this.major}.{this.minor}.{this.build}.{this.revision}";
            #endif

            //Save the new build to the disk
            SaveToFile();

            //If the version must be uploaded, do it now
            if (!string.IsNullOrEmpty(uploadURL))
            {
                BuildVersionWebClient.PostBuildVersion(uploadURL, this);
            }
        }

        /// <summary>
        /// Saves this BuildVersion to the disk
        /// </summary>
        public void SaveToFile()
        {
            #if DEBUG
            string path = Path.Combine(BuildToolUtils.DataPath, BuildToolUtils.ProductName.ToLowerInvariant() + EXTENSION);
            #else
            string path = BuildToolWindow.BuildFilePath;
            #endif
            File.WriteAllText(path, ToString(), Encoding.ASCII);
        }

        /// <summary>
        ///A string version of the build
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"{this.VersionString}|{this.Author}@{this.BuildDateString}";

        /// <summary>
        /// Gets an info string from the BuildVersion
        /// </summary>
        /// <returns>Nicely formatted info string string</returns>
        public string InfoString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Version: " + this.VersionString);
            sb.AppendLine("Build Time: " + this.BuildTime.ToString(TIME_FORMAT, CultureInfo.InvariantCulture));
            sb.AppendLine("Author: " + this.Author);
            return sb.ToString();
        }
        #endregion
    }
}
