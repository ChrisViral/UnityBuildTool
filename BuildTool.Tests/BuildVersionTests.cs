using System;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using NUnit.Framework;
using VersionBump = BuildTool.BuildVersion.VersionBump;

namespace BuildTool.Tests
{
    /// <summary>
    /// BuildVersion Unit tests
    /// </summary>
    [TestFixture(Author = "stupid_chris", Category = "Unit", TestOf = typeof(BuildVersion))]
    public class BuildVersionTests
    {
        #region Constants
        /// <summary>
        /// The valid author characters
        /// </summary>
        private const string charSet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_";
        /// <summary>
        /// RNG for this test class
        /// </summary>
        private static readonly Random rng = new Random();
        /// <summary>
        /// Path to the build file
        /// </summary>
        private static readonly string path = Path.Combine(BuildToolUtils.DataPath, BuildToolUtils.ProductName.ToLowerInvariant() + BuildVersion.EXTENSION);
        #endregion

        #region Fields
        //Version info
        private int major, minor, build, revision;
        private string author;
        private DateTime time;
        private string expected;
        private BuildVersion version;
        #endregion

        #region Tests
        [SetUp]
        public void Setup()
        {
            //Generate random version number
            this.major = rng.Next(0, 10);
            this.minor = rng.Next(0, 10);
            this.build = rng.Next(0, 20);
            this.revision = rng.Next(1, 100);
            //Generate random author
            this.author = TestUtils.GetRandomString(charSet, rng.Next(5, 15));
            //Get a random DateTime
            this.time = TestUtils.GetRandomDate();
            //Get expected BuildVersion string
            this.expected = $"v{this.major}.{this.minor}.{this.build:#0}.{this.revision:#0}|{this.author}@{this.time.Day:00}/{this.time.Month:00}/{this.time.Year:0000}-{this.time.Hour:00}:{this.time.Minute:00}:{this.time.Second:00}UTC";
            //Create the version object
            this.version = new BuildVersion(this.major, this.minor, this.build, this.revision, this.author, this.time);
        }

        [Test(Author = "stupid_chris", TestOf = typeof(BuildVersion)), Repeat(5), Order(0)]
        public void ToStringTest()
        {
            //Assert both are equal
            Assert.AreEqual(this.expected, this.version.ToString());
        }

        [Test(Author = "stupid_chris", TestOf = typeof(BuildVersion)), Repeat(5), Order(1)]
        public void SaveToFileTest()
        {
            //Save to file
            this.version.SaveToFile();
            //Test if the file exists and is as expected
            Assert.Multiple(() =>
            {
                Assert.IsTrue(File.Exists(path));
                Assert.AreEqual(this.expected, File.ReadAllText(path, Encoding.ASCII));
            });
        }

        [Test(Author = "stupid_chris", TestOf = typeof(BuildVersion)), Repeat(5), Order(2)]
        public void FromFileTest()
        {
            //Write a test file and try to load it
            File.WriteAllText(path, this.expected, Encoding.ASCII);
            BuildVersion loaded = BuildVersion.FromFile();
            //Test if the loaded object is equal
            Assert.Multiple(() =>
            {
                Assert.AreEqual(this.version.Version, loaded.Version);
                Assert.AreEqual(this.version.Author, loaded.Author);
                Assert.AreEqual(this.version.BuildTime, loaded.BuildTime);
            });
        }

        [Test(Author = "stupid_chris", TestOf = typeof(BuildVersion)), Repeat(5), Order(3)]
        public void GetBumpedVersionStringTest([Range(0, 3, 1)] int b)
        {
            //Get bump
            VersionBump bump = (VersionBump)b;
            //Bump necessary numbers
            this.revision++;
            switch (bump)
            {
                case VersionBump.Major:
                    this.major++;
                    this.minor = this.build = 0;
                    break;
                case VersionBump.Minor:
                    this.minor++;
                    this.build = 0;
                    break;
                case VersionBump.Build:
                    this.build++;
                    break;
            }

            //Check the resulting strings are equal
            Assert.AreEqual("v" + new Version(this.major, this.minor, this.build, this.revision).ToString(4), this.version.GetBumpedVersionString(bump));
        }

        [Test(Author = "stupid_chris", TestOf = typeof(BuildVersion)), Repeat(5), Order(4)]
        public void BuildTest([Range(0, 3, 1)] int b)
        {
            //Get bump
            VersionBump bump = (VersionBump)b;
            //Get info
            string versionString = this.version.GetBumpedVersionString(bump); //We can use this because we already tested it
            string newAuthor = TestUtils.GetRandomString(charSet, rng.Next(5, 15));
            DateTime now = DateTime.UtcNow;

            //Build it
            this.version.Build(bump, newAuthor);

            //Check if all is good
            Assert.Multiple(() =>
            {
                Assert.AreEqual(newAuthor, this.version.Author);
                Assert.AreEqual(versionString, this.version.VersionString);
                Assert.LessOrEqual(now, this.version.BuildTime);
                Assert.IsTrue(File.Exists(path));
                Assert.AreEqual(this.version.ToString(), File.ReadAllText(path, Encoding.ASCII));
            });
        }

        [Test(Author = "stupid_chris", TestOf = typeof(BuildVersion)), Repeat(5), Order(5)]
        public async Task JsonSerializeTest()
        {
            //Set the serialized expected JSON
            string serialized = ("{" +
                                   "  'version': {" +
                                  $"    'major': {this.major}," +
                                  $"    'minor': {this.minor}," +
                                  $"    'build': {this.build}," +
                                  $"    'revision': {this.revision}" +
                                   "  }," +
                                  $"  'build_time': '{this.version.BuildDateString}'," +
                                  $"  'author': '{this.author}'" +
                                   " }")
                                  .Replace('\'', '"')           //Replacing single quotes for double quotes
                                  .Replace(" ", string.Empty);  //Removing all spaces

            using (MemoryStream stream = new MemoryStream())
            {
                //Create the serializer
                DataContractJsonSerializerSettings settings = new DataContractJsonSerializerSettings { DateTimeFormat = new DateTimeFormat(BuildVersion.TIME_FORMAT, CultureInfo.InvariantCulture) };
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(BuildVersion), settings);
                //Serialize to the stream, then return to the origin
                serializer.WriteObject(stream, this.version);
                stream.Seek(0, SeekOrigin.Begin);
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    //Make sure the serialized data equals what we expect
                    Assert.AreEqual(serialized, (await reader.ReadToEndAsync()).Replace("\\", string.Empty));
                }
            }
        }

        [Test(Author = "stupid_chris", TestOf = typeof(BuildVersion)), Repeat(5), Order(6)]
        public async Task JsonDeserializeTest()
        {
            //Set the serialized expected JSON
            string serialized = ("{" +
                                 "  'version': {" +
                                $"    'major': {this.major}," +
                                $"    'minor': {this.minor}," +
                                $"    'build': {this.build}," +
                                $"    'revision': {this.revision}" +
                                 "  }," +
                                $"  'build_time': '{this.version.BuildDateString}'," +
                                $"  'author': '{this.author}'" +
                                 " }")
                                .Replace('\'', '"');         //Replacing single quotes for double quotes

            BuildVersion deserialized;
            using (MemoryStream stream = new MemoryStream())
            {
                //Write the serialized data to the stream
                using (StreamWriter writer = new StreamWriter(stream, Encoding.ASCII, 1024, true))
                {
                    await writer.WriteAsync(serialized);
                }
                //Create the serializer
                DataContractJsonSerializerSettings settings = new DataContractJsonSerializerSettings { DateTimeFormat = new DateTimeFormat(BuildVersion.TIME_FORMAT, CultureInfo.InvariantCulture) };
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(BuildVersion), settings);
                stream.Seek(0, SeekOrigin.Begin);
                //Read it back into an object
                deserialized = (BuildVersion)serializer.ReadObject(stream);
            }

            //Test if the deserialized object is equal
            Assert.Multiple(() =>
            {
                Assert.AreEqual(this.version.Version, deserialized.Version);
                Assert.AreEqual(this.version.Author, deserialized.Author);
                Assert.AreEqual(this.version.BuildTime, deserialized.BuildTime);
            });
        }

        [TearDown]
        public void Teardown()
        {
            //Delete the build file if it exists
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        #endregion
    }
}