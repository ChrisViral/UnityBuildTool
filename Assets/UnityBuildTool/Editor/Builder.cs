using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UnityBuildTool
{
    /// <summary>
    /// Unity builder class
    /// </summary>
    public static class Builder
    {
        #region Static methods
        /// <summary>
        /// Builds all the players for the given targets
        /// </summary>
        /// <param name="settings">The settings to build for</param>
        /// <param name="targets">Targets to build for</param>
        /// <param name="success">An array containing the success of each build</param>
        /// <param name="resetTarget">If the buildTarget should be reset at the end of the build, defaults to <see langword="true"/></param>
        /// <exception cref="ArgumentNullException">If <paramref name="settings"/> or <paramref name="targets"/> is <see langword="null"/></exception>
        public static void BuildAll(BuildToolSettings settings, BuildTarget[] targets, out bool[] success, bool resetTarget = true)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings), "Build Settings cannot be null");
            if (targets is null) throw new ArgumentNullException(nameof(targets), "Build targets array cannot be null");

            if (targets.Length == 0)
            {
                //Nothing to do if the targets array is empty
                success = Array.Empty<bool>();
                return;
            }

            //Get original info in main thread
            BuildTarget originalTarget = EditorUserBuildSettings.activeBuildTarget;
            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();

            //Progressbar
            int current = 0;
            float total = targets.Length;
            success = new bool[targets.Length];
            foreach (BuildTarget target in targets)
            {
                if (EditorUtility.DisplayCancelableProgressBar("Building Player", "Building " + BuildToolUtils.GetBuildTargetName(target), current / total))
                {
                    Debug.LogWarning("[Builder]: Build process cancelled");
                    break;
                }

                try
                {
                    Build(settings, target, scenes);
                    success[current] = true;
                }
                catch (BuildFailedException e)
                {
                    Debug.LogException(e);
                }
                current++;
            }

            //Make sure the progressbar is cleared
            EditorUtility.ClearProgressBar();
            //Put the target back to the original
            if (resetTarget && EditorUserBuildSettings.activeBuildTarget != originalTarget)
            {
                EditorUserBuildSettings.SwitchActiveBuildTargetAsync(BuildPipeline.GetBuildTargetGroup(originalTarget), originalTarget);
            }
        }

        /// <summary>
        /// Builds the game for the given platform
        /// </summary>
        /// <param name="settings">Settings to build for</param>
        /// <param name="target">Target to build</param>
        /// <param name="scenes">Array of scenes to include in the build</param>
        /// <exception cref="BuildFailedException">If the build failed for any reason</exception>
        public static void Build(BuildToolSettings settings, BuildTarget target, string[] scenes)
        {
            //Get some info on the build
            string targetName = BuildToolUtils.GetBuildTargetName(target);
            string parentDirectory = Path.Combine(BuildToolUtils.ProjectFolderPath, settings.OutputFolder, targetName);
            //Create the build options
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(parentDirectory, BuildToolUtils.ProductName),
                target = target,
                options = settings.DevelopmentBuild ? BuildOptions.Development : BuildOptions.None
            };
            //If on Windows, must add extension manually
            if (target == BuildTarget.StandaloneWindows || target == BuildTarget.StandaloneWindows64)
            {
                options.locationPathName += ".exe";
            }

            //Delete the existing build directory if any
            if (Directory.Exists(parentDirectory))
            {
                Directory.Delete(parentDirectory, true);
            }

            //Build player
            BuildSummary summary = BuildPipeline.BuildPlayer(options).summary;
            //Throw if any errors happened
            if (summary.result != BuildResult.Succeeded) throw new BuildFailedException($"{summary.totalErrors} errors happened while building for {targetName}");

            //Display success message
            Debug.Log("[Builder]: Build completed successfully for platform " + targetName);
        }
        #endregion
    }
}