using System;
using System.Globalization;
using Newtonsoft.Json;

namespace BuildTool.Json
{
    /// <summary>
    /// DateTime JSON converter for the build format
    /// </summary>
    public class VersionDateConverter : JsonConverter<DateTime>
    {
        #region Constants
        /// <summary>
        /// DateTime format used
        /// </summary>
        private const string format = "dd/MM/yyyy-HH:mm:ssUTC";
        #endregion

        #region Methods
        /// <summary>Writes the JSON representation of the object.</summary>
        /// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter" /> to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, DateTime value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString(format, CultureInfo.InvariantCulture));
        }

        /// <summary>Reads the JSON representation of the object.</summary>
        /// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader" /> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read. If there is no existing value then <c>null</c> will be used.</param>
        /// <param name="hasExistingValue">The existing value has a value.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>The object value.</returns>
        public override DateTime ReadJson(JsonReader reader, Type objectType, DateTime existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return DateTime.SpecifyKind(DateTime.ParseExact((string)reader.Value, format, CultureInfo.InvariantCulture), DateTimeKind.Utc);
        }
        #endregion
    }
}
