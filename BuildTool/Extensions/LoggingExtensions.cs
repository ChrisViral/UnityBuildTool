﻿using System;
#if !DEBUG
using UnityEngine;
using Object = UnityEngine.Object;
#endif

namespace BuildTool.Extensions
{
    /// <summary>
    /// Unity objects extension methods
    /// </summary>
    public static class LoggingExtensions
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
    }
}
