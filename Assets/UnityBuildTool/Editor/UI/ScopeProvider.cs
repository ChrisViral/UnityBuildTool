using System;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;

namespace UnityBuildTool.UI
{
    public class HorizontalScope : IDisposable
    {
        private static readonly HorizontalScope provider = new HorizontalScope();

        private HorizontalScope() { }

        public static HorizontalScope Enter(params GUILayoutOption[] options)
        {
            EditorGUILayout.BeginHorizontal(options);
            return provider;
        }

        public static HorizontalScope Enter(GUIStyle style, params GUILayoutOption[] options)
        {
            EditorGUILayout.BeginHorizontal(style, options);
            return provider;
        }

        public void Dispose() => EditorGUILayout.EndHorizontal();
    }

    public class VerticalScope : IDisposable
    {
        private static readonly VerticalScope provider = new VerticalScope();

        private VerticalScope() { }

        public static VerticalScope Enter(params GUILayoutOption[] options)
        {
            EditorGUILayout.BeginVertical(options);
            return provider;
        }

        public static VerticalScope Enter(GUIStyle style, params GUILayoutOption[] options)
        {
            EditorGUILayout.BeginVertical(style, options);
            return provider;
        }

        public void Dispose() => EditorGUILayout.EndVertical();
    }

    public class FadeGroupScope : IDisposable
    {
        private static readonly FadeGroupScope provider = new FadeGroupScope();

        private FadeGroupScope() { }

        public static FadeGroupScope Enter(float faded, out bool visible)
        {
            visible = EditorGUILayout.BeginFadeGroup(faded);
            return provider;
        }

        public void Dispose() => EditorGUILayout.EndFadeGroup();
    }

    public class ScrollViewScope : IDisposable
    {
        private static readonly ScrollViewScope provider = new ScrollViewScope();

        private ScrollViewScope() { }

        public static ScrollViewScope Enter(ref Vector2 scrollPosition, params GUILayoutOption[] options)
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, options);
            return provider;
        }

        public static ScrollViewScope Enter(ref Vector2 scrollPosition, GUIStyle style, params GUILayoutOption[] options)
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, style, options);
            return provider;
        }

        public static ScrollViewScope Enter(ref Vector2 scrollPosition, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, params GUILayoutOption[] options)
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, horizontalScrollbar, verticalScrollbar, options);
            return provider;
        }

        public static ScrollViewScope Enter(ref Vector2 scrollPosition, bool alwaysShowVertical, bool alwaysShowHorizontal, params GUILayoutOption[] options)
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, alwaysShowVertical, alwaysShowHorizontal, options);
            return provider;
        }

        public static ScrollViewScope Enter(ref Vector2 scrollPosition, bool alwaysShowVertical, bool alwaysShowHorizontal, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, GUIStyle background, params GUILayoutOption[] options)
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, alwaysShowVertical, alwaysShowHorizontal, horizontalScrollbar, verticalScrollbar, background, options);
            return provider;
        }

        public void Dispose() => EditorGUILayout.EndScrollView();
    }

    public class FoldoutHeaderScope : IDisposable
    {
        private static readonly FoldoutHeaderScope provider = new FoldoutHeaderScope();

        private FoldoutHeaderScope() { }

        public static FoldoutHeaderScope Enter(AnimBool animBool, string content, GUIStyle style = null, Action<Rect> menuAction = null, GUIStyle menuIcon = null)
        {
            animBool.target = EditorGUILayout.BeginFoldoutHeaderGroup(animBool.target, content, style, menuAction, menuIcon);
            return provider;
        }

        public static FoldoutHeaderScope Enter(AnimBool animBool, GUIContent content, GUIStyle style = null, Action<Rect> menuAction = null, GUIStyle menuIcon = null)
        {
            animBool.target = EditorGUILayout.BeginFoldoutHeaderGroup(animBool.target, content, style, menuAction, menuIcon);
            return provider;
        }

        public static FoldoutHeaderScope Enter(ref bool foldout, string content, GUIStyle style = null, Action<Rect> menuAction = null, GUIStyle menuIcon = null)
        {
            foldout = EditorGUILayout.BeginFoldoutHeaderGroup(foldout, content, style, menuAction, menuIcon);
            return provider;
        }

        public static FoldoutHeaderScope Enter(ref bool foldout, GUIContent content, GUIStyle style = null, Action<Rect> menuAction = null, GUIStyle menuIcon = null)
        {
            foldout = EditorGUILayout.BeginFoldoutHeaderGroup(foldout, content, style, menuAction, menuIcon);
            return provider;
        }

        public void Dispose() => EditorGUILayout.EndFoldoutHeaderGroup();
    }
}
