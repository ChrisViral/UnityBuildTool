using System;
using System.Collections.Generic;
using System.Linq;
#if !DEBUG
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
#endif

namespace BuildTool.Extensions
{
    /// <summary>
    /// Collection of extension methods
    /// </summary>
    public static class Extensions
    {
        #region Logging extensions
        #if DEBUG
        /// <summary>
        /// Logs an object message
        /// </summary>
        /// <param name="o">Object that is logging</param>
        /// <param name="message">Message to log</param>
        public static void Log(this object o, object message) => Console.WriteLine($"[{o.GetType().Name}]: {message}");

        /// <summary>
        /// Logs a given warning message
        /// </summary>
        /// <param name="o">Object that is logging</param>
        /// <param name="message">Message to log</param>
        public static void LogWarning(this object o, object message) => Console.WriteLine($"WARNING-[{o.GetType().Name}]: {message}");

        /// <summary>
        /// Logs a given error message
        /// </summary>
        /// <param name="o">Object that is logging</param>
        /// <param name="message">Message to log</param>
        public static void LogError(this object o, object message) => Console.WriteLine($"ERROR-[{o.GetType().Name}]: {message}");

        /// <summary>
        /// Logs an exception
        /// </summary>
        /// <param name="o">Object that is logging</param>
        /// <param name="e">Exception to log</param>
        public static void LogException(this object o, Exception e) => Console.Error.WriteLine($"{e.GetType()}: {e.Message}\n{e.StackTrace}");
        #else
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
        #endif
        #endregion

        #region IEnumerable Extension
        /// <summary>
        /// Wraps this object instance into an <see cref="IEnumerable{T}"/> consisting of a single item.
        /// </summary>
        /// <typeparam name="T">Type of the object</typeparam>
        /// <param name="item">The instance that will be wrapped.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> consisting of a single item.</returns>
        public static IEnumerable<T> Yield<T>(this T item)
        {
            yield return item;
        }
        #endregion

        #if !DEBUG
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
            return property.Children().FirstOrDefault(p => p.name == name && p.propertyType == SerializedPropertyType.String)?.stringValue == value;
        }
        #endregion
        #endif
    }
}
