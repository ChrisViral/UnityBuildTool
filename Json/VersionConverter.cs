using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BuildTool.Json
{
    /// <summary>
    /// Version JSON converter
    /// </summary>
    public class VersionConverter : JsonConverter<Version>
    {
        #region Methods
        /// <summary>Writes the JSON representation of the object.</summary>
        /// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter" /> to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, Version value, JsonSerializer serializer)
        {
            new JObject
            {
                ["major"]    = value.Major,
                ["minor"]    = value.Minor,
                ["build"]    = value.Build,
                ["revision"] = value.Revision
            }.WriteTo(writer);
        }

        /// <summary>Reads the JSON representation of the object.</summary>
        /// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader" /> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read. If there is no existing value then <c>null</c> will be used.</param>
        /// <param name="hasExistingValue">The existing value has a value.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>The object value.</returns>
        public override Version ReadJson(JsonReader reader, Type objectType, Version existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject version = JObject.Load(reader);
            return new Version($"{version["major"]}.{version["minor"]}.{version["build"]}.{version["revision"]}");
        }
        #endregion
    }
}
