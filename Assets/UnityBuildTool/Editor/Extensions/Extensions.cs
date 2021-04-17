using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityBuildTool.Extensions
{
    /// <summary>
    /// Collection of extension methods
    /// </summary>
    public static class Extensions
    {
        #region Logging extensions
        /// <summary>
        /// Logs an object message
        /// </summary>
        /// <param name="o">Object that is logging</param>
        /// <param name="message">Message to log</param>
        public static void Log(this object o, object message) => Debug.Log($"[{o.GetType().Name}]: {message}");

        /// <summary>
        /// Logs a given warning message
        /// </summary>
        /// <param name="o">Object that is logging</param>
        /// <param name="message">Message to log</param>
        public static void LogWarning(this object o, object message) => Debug.LogWarning($"[{o.GetType().Name}]: {message}");

        /// <summary>
        /// Logs a given error message
        /// </summary>
        /// <param name="o">Object that is logging</param>
        /// <param name="message">Message to log</param>
        public static void LogError(this object o, object message) => Debug.LogError($"[{o.GetType().Name}]: {message}");

        /// <summary>
        /// Logs an exception
        /// </summary>
        /// <param name="o">Object that is logging</param>
        /// <param name="e">Exception to log</param>
        public static void LogException(this object o, Exception e) => Debug.LogException(e);

        /// <summary>
        /// Logs an object message
        /// </summary>
        /// <param name="o">Unity object that is logging</param>
        /// <param name="message">Message to log</param>
        public static void Log(this Object o, object message) => Debug.Log($"[{o.GetType().Name}]: {message}", o);

        /// <summary>
        /// Logs a given warning message
        /// </summary>
        /// <param name="o">Unity object that is logging</param>
        /// <param name="message">Message to log</param>
        public static void LogWarning(this Object o, object message) => Debug.LogWarning($"[{o.GetType().Name}]: {message}", o);

        /// <summary>
        /// Logs a given error message
        /// </summary>
        /// <param name="o">Unity object that is logging</param>
        /// <param name="message">Message to log</param>
        public static void LogError(this Object o, object message) => Debug.LogError($"[{o.GetType().Name}]: {message}", o);

        /// <summary>
        /// Logs an exception
        /// </summary>
        /// <param name="o">Unity object that is logging</param>
        /// <param name="e">Exception to log</param>
        public static void LogException(this Object o, Exception e) => Debug.LogException(e, o);
        #endregion

        #region IEnumerable Extension
        /// <summary>
        /// Enumerates this directory and all it's subdirectories, recursively
        /// </summary>
        /// <param name="directory">Directory to enumerate</param>
        /// <returns>An enumerable of all directories found from the root</returns>
        public static IEnumerable<DirectoryInfo> EnumerateAllDirectories(this DirectoryInfo directory)
        {
            yield return directory;
            foreach (DirectoryInfo subDirectory in directory.EnumerateDirectories("*", SearchOption.AllDirectories))
            {
                yield return subDirectory;
            }
        }
        #endregion

        #region SerializedProperty extensions
        /// <summary>
        /// Enumerates all the children of this property
        /// </summary>
        /// <param name="parent">Parent property</param>
        /// <returns>An Enumerator of all the children of this property</returns>
        public static IEnumerable<SerializedProperty> Children(this SerializedProperty parent)
        {
            //Copy the property, then yield all children
            foreach (SerializedProperty child in parent)
            {
                yield return child;
            }
        }

        /// <summary>
        /// Checks if a SerializedProperty contains a given value
        /// </summary>
        /// <param name="property">Property to check</param>
        /// <param name="name">Name of the property to find</param>
        /// <param name="value">Value to find</param>
        /// <returns>True if the value is contained in the SerializedProperty's children, false otherwise</returns>
        public static bool Contains(this SerializedProperty property, string name, string value)
        {
            //Get the first string property with the given name
            return property.Children().Any(p => p.name == name && p.propertyType == SerializedPropertyType.String && p.stringValue == value);
        }
        #endregion
    }
}
