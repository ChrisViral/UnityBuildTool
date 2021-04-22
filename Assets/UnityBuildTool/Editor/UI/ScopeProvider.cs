using System;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;

namespace UnityBuildTool.UI
{
    /// <summary>
    /// Horizontal scope provider
    /// </summary>
    public class HorizontalScope : IDisposable
    {
        private static readonly IDisposable provider = new HorizontalScope();

        /// <summary>
        /// Prevents instantiation
        /// </summary>
        private HorizontalScope() { }

        /// <summary>
        /// Enters a new horizontal scope
        /// </summary>
        /// <param name="options">Layout options</param>
        /// <returns>The disposable implementation that exists the scope</returns>
        public static IDisposable Enter(params GUILayoutOption[] options)
        {
            EditorGUILayout.BeginHorizontal(options);
            return provider;
        }

        /// <summary>
        /// Enters a new horizontal scope
        /// </summary>
        /// <param name="style">Style of the scope</param>
        /// <param name="options">Layout options</param>
        /// <returns>The disposable implementation that exists the scope</returns>
        public static IDisposable Enter(GUIStyle style, params GUILayoutOption[] options)
        {
            EditorGUILayout.BeginHorizontal(style, options);
            return provider;
        }

        /// <summary>
        /// Exits the latest scope
        /// </summary>
        public void Dispose() => EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// Vertical scope provider
    /// </summary>
    public class VerticalScope : IDisposable
    {
        private static readonly IDisposable provider = new VerticalScope();

        /// <summary>
        /// Prevents instantiation
        /// </summary>
        private VerticalScope() { }

        /// <summary>
        /// Enters a new vertical scope
        /// </summary>
        /// <param name="options">Layout options</param>
        /// <returns>The disposable implementation that exists the scope</returns>
        public static IDisposable Enter(params GUILayoutOption[] options)
        {
            EditorGUILayout.BeginVertical(options);
            return provider;
        }

        /// <summary>
        /// Enters a new vertical scope
        /// </summary>
        /// <param name="style">Style of the scope</param>
        /// <param name="options">Layout options</param>
        /// <returns>The disposable implementation that exists the scope</returns>
        public static IDisposable Enter(GUIStyle style, params GUILayoutOption[] options)
        {
            EditorGUILayout.BeginVertical(style, options);
            return provider;
        }

        /// <summary>
        /// Exits the latest scope
        /// </summary>
        public void Dispose() => EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// Fade group scope provider
    /// </summary>
    public class FadeGroupScope : IDisposable
    {
        private static readonly IDisposable provider = new FadeGroupScope();

        /// <summary>
        /// Prevents instantiation
        /// </summary>
        private FadeGroupScope() { }

        /// <summary>
        /// Enters a new fade group scope
        /// </summary>
        /// <param name="faded">Faded amount</param>
        /// <param name="visible">If the contents of the fade group are visible or not</param>
        /// <returns>The disposable implementation that exists the scope</returns>
        public static IDisposable Enter(float faded, out bool visible)
        {
            visible = EditorGUILayout.BeginFadeGroup(faded);
            return provider;
        }

        /// <summary>
        /// Exits the latest scope
        /// </summary>
        public void Dispose() => EditorGUILayout.EndFadeGroup();
    }

    /// <summary>
    /// Scroll view scope provider
    /// </summary>
    public class ScrollViewScope : IDisposable
    {
        private static readonly IDisposable provider = new ScrollViewScope();

        /// <summary>
        /// Prevents instantiation
        /// </summary>
        private ScrollViewScope() { }

        /// <summary>
        /// Enters a new scroll view scope
        /// </summary>
        /// <param name="scrollPosition">Reference to the current scroll position</param>
        /// <param name="options">Layout options</param>
        /// <returns>The disposable implementation that exists the scope</returns>
        public static IDisposable Enter(ref Vector2 scrollPosition, params GUILayoutOption[] options)
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, options);
            return provider;
        }

        /// <summary>
        /// Enters a new scroll view scope
        /// </summary>
        /// <param name="scrollPosition">Reference to the current scroll position</param>
        /// <param name="style">GUIStyle for the scroll view</param>
        /// <param name="options">Layout options</param>
        /// <returns>The disposable implementation that exists the scope</returns>
        public static IDisposable Enter(ref Vector2 scrollPosition, GUIStyle style, params GUILayoutOption[] options)
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, style, options);
            return provider;
        }

        /// <summary>
        /// Enters a new scroll view scope
        /// </summary>
        /// <param name="scrollPosition">Reference to the current scroll position</param>
        /// <param name="horizontalScrollbar">GUIStyle for the horizontal scrollbar</param>
        /// <param name="verticalScrollbar">GUIStyle for the vertical scrollbar</param>
        /// <param name="options">Layout options</param>
        /// <returns>The disposable implementation that exists the scope</returns>
        public static IDisposable Enter(ref Vector2 scrollPosition, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, params GUILayoutOption[] options)
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, horizontalScrollbar, verticalScrollbar, options);
            return provider;
        }

        /// <summary>
        /// Enters a new scroll view scope
        /// </summary>
        /// <param name="scrollPosition">Reference to the current scroll position</param>
        /// <param name="alwaysShowHorizontal">If the horizontal scrollbar should always be shown</param>
        /// <param name="alwaysShowVertical">If the vertical scrollbar should always be shown</param>
        /// <param name="options">Layout options</param>
        /// <returns>The disposable implementation that exists the scope</returns>
        public static IDisposable Enter(ref Vector2 scrollPosition, bool alwaysShowHorizontal, bool alwaysShowVertical, params GUILayoutOption[] options)
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, alwaysShowHorizontal, alwaysShowVertical, options);
            return provider;
        }

        /// <summary>
        /// Enters a new scroll view scope
        /// </summary>
        /// <param name="scrollPosition">Reference to the current scroll position</param>
        /// <param name="alwaysShowHorizontal">If the horizontal scrollbar should always be shown</param>
        /// <param name="alwaysShowVertical">If the vertical scrollbar should always be shown</param>
        /// <param name="horizontalScrollbar">GUIStyle for the horizontal scrollbar</param>
        /// <param name="verticalScrollbar">GUIStyle for the vertical scrollbar</param>
        /// <param name="background">GUIStyle for the scrollview background</param>
        /// <param name="options">Layout options</param>
        /// <returns>The disposable implementation that exists the scope</returns>
        public static IDisposable Enter(ref Vector2 scrollPosition, bool alwaysShowHorizontal, bool alwaysShowVertical, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, GUIStyle background, params GUILayoutOption[] options)
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, alwaysShowHorizontal, alwaysShowVertical, horizontalScrollbar, verticalScrollbar, background, options);
            return provider;
        }

        /// <summary>
        /// Exits the latest scope
        /// </summary>
        public void Dispose() => EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// Foldout header scope provider
    /// </summary>
    public class FoldoutHeaderScope : IDisposable
    {
        private static readonly IDisposable provider = new FoldoutHeaderScope();

        /// <summary>
        /// Prevents instantiation
        /// </summary>
        private FoldoutHeaderScope() { }

        /// <summary>
        /// Enters a new foldout header scope
        /// </summary>
        /// <param name="animBool">AnimBool attached to this foldout</param>
        /// <param name="content">Foldout label content</param>
        /// <param name="style">Foldout style</param>
        /// <param name="menuAction">Action of the foldout menu</param>
        /// <param name="menuIcon">Menu icon style</param>
        /// <returns>The disposable implementation that exists the scope</returns>
        public static IDisposable Enter(AnimBool animBool, string content, GUIStyle style = null, Action<Rect> menuAction = null, GUIStyle menuIcon = null)
        {
            animBool.target = EditorGUILayout.BeginFoldoutHeaderGroup(animBool.target, content, style, menuAction, menuIcon);
            return provider;
        }

        /// <summary>
        /// Enters a new foldout header scope
        /// </summary>
        /// <param name="animBool">AnimBool attached to this foldout</param>
        /// <param name="content">Foldout label content</param>
        /// <param name="style">Foldout style</param>
        /// <param name="menuAction">Action of the foldout menu</param>
        /// <param name="menuIcon">Menu icon style</param>
        /// <returns>The disposable implementation that exists the scope</returns>
        public static IDisposable Enter(AnimBool animBool, GUIContent content, GUIStyle style = null, Action<Rect> menuAction = null, GUIStyle menuIcon = null)
        {
            animBool.target = EditorGUILayout.BeginFoldoutHeaderGroup(animBool.target, content, style, menuAction, menuIcon);
            return provider;
        }

        /// <summary>
        /// Enters a new foldout header scope
        /// </summary>
        /// <param name="foldout">Reference to the foldout's current toggled value</param>
        /// <param name="content">Foldout label content</param>
        /// <param name="style">Foldout style</param>
        /// <param name="menuAction">Action of the foldout menu</param>
        /// <param name="menuIcon">Menu icon style</param>
        /// <returns>The disposable implementation that exists the scope</returns>
        public static IDisposable Enter(ref bool foldout, string content, GUIStyle style = null, Action<Rect> menuAction = null, GUIStyle menuIcon = null)
        {
            foldout = EditorGUILayout.BeginFoldoutHeaderGroup(foldout, content, style, menuAction, menuIcon);
            return provider;
        }

        /// <summary>
        /// Enters a new foldout header scope
        /// </summary>
        /// <param name="foldout">Reference to the foldout's current toggled value</param>
        /// <param name="content">Foldout label content</param>
        /// <param name="style">Foldout style</param>
        /// <param name="menuAction">Action of the foldout menu</param>
        /// <param name="menuIcon">Menu icon style</param>
        /// <returns>The disposable implementation that exists the scope</returns>
        public static IDisposable Enter(ref bool foldout, GUIContent content, GUIStyle style = null, Action<Rect> menuAction = null, GUIStyle menuIcon = null)
        {
            foldout = EditorGUILayout.BeginFoldoutHeaderGroup(foldout, content, style, menuAction, menuIcon);
            return provider;
        }

        /// <summary>
        /// Exits the latest scope
        /// </summary>
        public void Dispose() => EditorGUILayout.EndFoldoutHeaderGroup();
    }
}
