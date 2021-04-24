﻿using System;
using System.Linq;
using Octokit;
using UnityBuildTool.Extensions;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;
using VersionBump = UnityBuildTool.BuildVersion.VersionBump;

namespace UnityBuildTool.UI
{
    /// <summary>
    /// Build handling UI
    /// </summary>
    public class BuildHandler
    {
        /// <summary>
        /// Struct containing all the necessary information to create a new release
        /// </summary>
        public readonly struct ReleaseSnapshot
        {
            #region Fields
            /// <summary>The VersionBump to apply</summary>
            public readonly VersionBump bump;
            /// <summary>The title of the release</summary>
            public readonly string title;
            /// <summary>The description of the release</summary>
            public readonly string description;
            /// <summary>The target commit's SHA</summary>
            public readonly string targetSHA;
            /// <summary>If this is a prerelease</summary>
            public readonly bool prerelease;
            /// <summary>If the release should be saved as a draft</summary>
            public readonly bool draft;
            #endregion

            #region Constructors
            /// <summary>
            /// Creates a new ReleaseSnapshot from the given BuildHandler
            /// </summary>
            /// <param name="handler">BuildHandler to create the snapshot from</param>
            public ReleaseSnapshot(BuildHandler handler)
            {
                //Copy over all needed data
                this.bump = handler.bump;
                this.title = handler.title;
                this.description = handler.description;
                this.targetSHA = handler.CurrentCommit.Sha;
                this.prerelease = handler.prerelease;
                this.draft = handler.draft;
            }
            #endregion

            #region Operators
            /// <summary>
            /// Implicitly converts a BuildHandler to a release snapshot
            /// </summary>
            /// <param name="handler">BuildHandler to create the snapshot from</param>
            public static implicit operator ReleaseSnapshot(BuildHandler handler) => new ReleaseSnapshot(handler);
            #endregion
        }

        #region Constants
        //GUIContent labels
        private static readonly GUIContent useURL               = new GUIContent("Use Version Service",   "If a webservice hosting the version object should be used");
        private static readonly GUIContent url                  = new GUIContent("Version URL:",          "URL of the web API where the version data is stored");
        private static readonly GUIContent connect              = new GUIContent("Connect",               "Connect to this web API to get and post build versions");
        private static readonly GUIContent header               = new GUIContent("New Build",             "Title of the GitHub release");
        private static readonly GUIContent descriptionLabel     = new GUIContent("Description",           "Description of the GitHub release (Markdown is supported)");
        private static readonly GUIContent branchSelector       = new GUIContent("Target",                "Branch to target");
        private static readonly GUIContent prereleaseToggle     = new GUIContent("Pre-release",           "If this build is a pre-release");
        private static readonly GUIContent draftToggle          = new GUIContent("Draft",                 "If the GitHub release should be saved as a draft");
        private static readonly GUIContent publishToggle        = new GUIContent("Publish Release",       "If the release should be published to GitHub or not");
        private static readonly GUIContent devBuildToggle       = new GUIContent("Development Build",     "If the player should be built by Unity as a development build");
        private static readonly GUIContent bumpLabel            = new GUIContent("Bump:",                  "How to bump the version number");
        private static readonly GUIContent outputDirectoryLabel = new GUIContent("Output Directory:",     "Local path to the directory where the output of the builds should be saved");
        private static readonly GUIContent buildTargetsLabel    = new GUIContent("Build targets:",        "Which platforms the game will be built for");
        private static readonly GUIContent buildButton          = new GUIContent("BUILD",                 "Build the game for the selected targets and release to GitHub");
        private static readonly GUIContent copyLabel            = new GUIContent("Files/Folders to copy", "Files and folders that should be copied over when building the game");
        private static readonly GUIContent deleteEntry          = new GUIContent("X",                     "Delete this entry to copy on build");
        private static readonly GUIContent loadedLabel          = new GUIContent("Waiting for valid BuildVersion from web API...");
        private static readonly GUIContent notLoadedLabel       = new GUIContent("Waiting for repository data to be loaded...");

        //Option arrays
        private static readonly GUILayoutOption[] selectorOptions         = { GUILayout.Width(425f) };
        private static readonly GUILayoutOption[] labelOptions            = { GUILayout.Width(109f) };
        private static readonly GUILayoutOption[] headerOptions           = { GUILayout.Width(150f), GUILayout.Height(30f) };
        private static readonly GUILayoutOption[] titleOptions            = { GUILayout.Height(25f) };
        private static readonly GUILayoutOption[] branchSelectorOptions   = { GUILayout.Width(175f) };
        private static readonly GUILayoutOption[] descriptionLabelOptions = { GUILayout.Width(100f), GUILayout.Height(30f) };
        private static readonly GUILayoutOption[] descriptionOptions      = { GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true) };
        private static readonly GUILayoutOption[] buildVersionOptions     = { GUILayout.Width(188f) };
        private static readonly GUILayoutOption[] sideButtonOptions       = { GUILayout.Width(70f) };
        private static readonly GUILayoutOption[] outputFolderOptions     = { GUILayout.Width(300f) };
        private static readonly GUILayoutOption[] targetsOptions          = { GUILayout.Width(375f) };
        private static readonly GUILayoutOption[] buildButtonOptions      = { GUILayout.Width(375f), GUILayout.Height(60f) };
        private static readonly GUILayoutOption[] filesScrollOptions      = { GUILayout.Width(400f), GUILayout.Height(200f) };
        private static readonly GUILayoutOption[] deleteButtonOptions     = { GUILayout.Width(20f) };
        private static readonly GUILayoutOption[] copyPopupOptions        = { GUILayout.Width(100f) };
        private static readonly GUILayoutOption[] addButtonOptions        = { GUILayout.Width(190f) };

        #endregion

        #region Fields
        //Objects
        private readonly BuildToolWindow window;
        private readonly SerializedProperty useWebService, versionURL, publishRelease, developmentBuild, outputFolder, targetFlags, copyOnBuild;
        private readonly GUILayoutOption[] loadedLabelOptions, notLoadedLabelOptions;

        //GUI Fields
        private readonly AnimBool urlUsed = new AnimBool(false);
        private string title, description, apiURL, currentCommitMessage, bumpString;
        private string[] branches;
        private bool prerelease, draft;
        private Vector2 scroll;
        #endregion

        #region Properties
        private int selectedBranch;
        /// <summary>
        /// Index of the currently selected branch
        /// </summary>
        private int SelectedBranch
        {
            get => this.selectedBranch;
            set
            {
                if (this.selectedBranch != value)
                {
                    this.selectedBranch = value;
                    this.CurrentCommit = this.window.Authenticator.CurrentBranches[this.selectedBranch].commit;
                }
            }
        }

        private GitHubCommit currentCommit;
        /// <summary>
        /// The last commit of the currently selected branch
        /// </summary>
        private GitHubCommit CurrentCommit
        {
            get => this.currentCommit;
            set
            {
                if (this.currentCommit != value)
                {
                    this.currentCommit = value;
                    this.currentCommitMessage = $"[{this.CurrentCommit.Sha.Substring(0, 7)}] {this.CurrentCommit.Commit.Message}";
                }
            }
        }

        private VersionBump bump;
        public VersionBump Bump
        {
            get => this.bump;
            set
            {
                if (this.bump != value)
                {
                    this.bump = value;
                    this.bumpString = this.window.BuildVersion.GetBumpedVersionString(value);
                }
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new BuildHandler Control attached to the given window
        /// </summary>
        /// <param name="window">BuildToolWindow to attach to</param>
        public BuildHandler(BuildToolWindow window)
        {
            //Set base stuff
            this.window = window;
            this.apiURL = this.window.Settings.VersionURL ?? string.Empty;
            this.urlUsed.value = window.Settings.UseWebService;
            this.urlUsed.valueChanged.AddListener(window.Repaint);

            //Settings serialized properties
            this.useWebService    = this.window.SerializedSettings.FindProperty(BuildToolSettings.USE_WEB_SERVICE_NAME);
            this.versionURL       = this.window.SerializedSettings.FindProperty(BuildToolSettings.VERSION_URL_NAME);
            this.publishRelease   = this.window.SerializedSettings.FindProperty(BuildToolSettings.PUBLISH_RELEASE_NAME);
            this.developmentBuild = this.window.SerializedSettings.FindProperty(BuildToolSettings.DEVELOPMENT_BUILD_NAME);
            this.outputFolder     = this.window.SerializedSettings.FindProperty(BuildToolSettings.OUTPUT_FOLDER_NAME);
            this.targetFlags      = this.window.SerializedSettings.FindProperty(BuildToolSettings.TARGET_FLAGS_NAME);
            this.copyOnBuild      = this.window.SerializedSettings.FindProperty(BuildToolSettings.COPY_ON_BUILD_NAME);

            //Label sizes
            this.loadedLabelOptions    = new[] { GUILayout.Width(EditorStyles.boldLabel.CalcSize(loadedLabel).x) };
            this.notLoadedLabelOptions = new[] { GUILayout.Width(EditorStyles.boldLabel.CalcSize(notLoadedLabel).x) };

            if (this.window.Settings.UseWebService)
            {
                //Try to get the build if there is a valid URL
                this.window.GetBuild();
            }
            else
            {
                //Else get it from the file
                this.window.GetBuildFromFile();
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Makes sure the branch selector is loaded correctly
        /// </summary>
        private void EnsureBranchesLoaded()
        {
            //Only run if the branch array is null
            if (this.branches is null)
            {
                //Get branch names
                this.branches = this.window.Authenticator.CurrentBranches.Select(t => t.branch.Name).ToArray();
                this.CurrentCommit = this.window.Authenticator.CurrentBranches[this.SelectedBranch].commit;
            }
        }

        /// <summary>
        /// Resets the release creation window to default
        /// </summary>
        public void Reset()
        {
            this.title = this.description = string.Empty;
            this.SelectedBranch = 0;
            this.bump = VersionBump.Revision;
            this.prerelease = this.draft = false;
        }

        /// <summary>
        /// Displays this control
        /// </summary>
        public void URLSelector()
        {
            using (HorizontalScope.Enter(selectorOptions))
            {
                using (VerticalScope.Enter())
                {
                    //Fade group
                    this.urlUsed.target = EditorGUILayout.ToggleLeft(useURL, this.urlUsed.target);
                    this.useWebService.boolValue = this.urlUsed.target;
                    if (string.IsNullOrEmpty(this.apiURL))
                    {
                        if (this.window.APIStatus != BuildToolWindow.BuildAPIStatus.NOT_CONNECTED && this.urlUsed.target)
                        {
                            this.versionURL.stringValue = string.Empty;
                            this.window.ClearBuild();
                        }
                    }
                    else if (!this.urlUsed.target)
                    {
                        this.versionURL.stringValue = string.Empty;
                        this.apiURL = string.Empty;
                    }

                    using (FadeGroupScope.Enter(this.urlUsed.faded, out bool visible))
                    {
                        if (visible)
                        {
                            //Get URL
                            EditorGUI.indentLevel++;
                            EditorGUIUtility.labelWidth = 110f;
                            this.apiURL = EditorGUILayout.TextField(url, this.apiURL);
                            EditorGUIUtility.labelWidth = 0f;
                            using (HorizontalScope.Enter())
                            {
                                //Display coloured connection label
                                (GUIContent label, GUIStyle style) = StylesUtils.ConnectionStyles[this.window.APIStatus];
                                EditorGUILayout.LabelField(label, style, labelOptions);
                                //Disable connection when the URL hasn't changed
                                GUI.enabled = this.window.UIEnabled && !string.IsNullOrEmpty(this.apiURL) && this.apiURL != this.window.Settings.VersionURL;
                                if (GUILayout.Button(connect))
                                {
                                    //Set on the serialized property
                                    this.versionURL.stringValue = this.apiURL;
                                    //Try to get the build
                                    this.Log("Testing connection to " + this.apiURL);
                                    this.window.GetBuild();
                                }

                                GUI.enabled = this.window.UIEnabled;
                            }

                            EditorGUI.indentLevel--;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Displays the Release creator UI
        /// </summary>
        public void ReleaseCreator()
        {
            //Only display UI if the branches have been loaded and the build version exists
            bool branchesNotLoaded = this.window.Authenticator.FetchingBranches || this.window.Authenticator.CurrentBranches is null;
            if (branchesNotLoaded || this.window.BuildVersion is null)
            {
                //Set the local branches to null to make sure the array is reinitialized when they have been fetched/repo has been changed
                this.branches = null;
                //Display info label
                GUIContent label = branchesNotLoaded ? notLoadedLabel : loadedLabel;
                GUILayoutOption[] options = branchesNotLoaded ? this.notLoadedLabelOptions : this.loadedLabelOptions;
                EditorGUILayout.LabelField(label, EditorStyles.boldLabel, options);
                //Repaint to make sure the change appears as soon as possible
                this.window.Repaint();
                return;
            }

            //Make sure the branch data is loaded
            EnsureBranchesLoaded();

            //Display the release UI
            GUI.enabled = this.window.UIEnabled && this.window.Settings.PublishRelease;

            //Header
            EditorGUILayout.LabelField(header, StylesUtils.TitleLabelStyle, headerOptions);
            EditorGUILayout.Space();
            //Title field
            this.title = EditorGUILayout.TextField(string.Empty, this.title, StylesUtils.TitleFieldStyle, titleOptions);
            EditorGUILayout.Space(EditorGUIUtility.standardVerticalSpacing * 2f);
            using (HorizontalScope.Enter())
            {

                GUI.enabled = this.window.UIEnabled && this.window.Settings.PublishRelease;
                //Branch selector
                EditorGUIUtility.labelWidth = 45f;
                this.SelectedBranch = EditorGUILayout.Popup(branchSelector, this.SelectedBranch, this.branches, branchSelectorOptions);
                //Repository label
                EditorGUIUtility.labelWidth = 15f;
                EditorGUILayout.LabelField("in", this.window.Authenticator.CurrentRepository.FullName, EditorStyles.boldLabel);
            }

            EditorGUIUtility.labelWidth = 80f;
            //Last commit on branch label
            EditorGUILayout.LabelField("Last commit:", this.currentCommitMessage);
            EditorGUIUtility.labelWidth = 0f;
            EditorGUILayout.Space();
            //Release description
            EditorGUILayout.LabelField(descriptionLabel, StylesUtils.DescriptionLabelStyle, descriptionLabelOptions);
            this.description = EditorGUILayout.TextArea(this.description, StylesUtils.DescriptionFieldStyle, descriptionOptions);

            //Display bottom Build UI
            using (HorizontalScope.Enter())
            {
                using (VerticalScope.Enter())
                {
                    //Prerelease/draft toggles
                    EditorGUILayout.Space(15f);
                    this.prerelease = EditorGUILayout.ToggleLeft(prereleaseToggle, this.prerelease);
                    this.draft = EditorGUILayout.ToggleLeft(draftToggle, this.draft);
                    GUI.enabled = this.window.UIEnabled;
                    //Publish toggle
                    this.publishRelease.boolValue = EditorGUILayout.ToggleLeft(publishToggle, this.publishRelease.boolValue);
                    //Dev build toggle
                    this.developmentBuild.boolValue = EditorGUILayout.ToggleLeft(devBuildToggle, this.developmentBuild.boolValue);
                    //Version bump settings
                    using (HorizontalScope.Enter())
                    {
                        EditorGUILayout.LabelField(bumpLabel, labelOptions);
                        GUI.enabled = false;
                        EditorGUILayout.TextField(this.bumpString, buildVersionOptions);
                        GUI.enabled = this.window.UIEnabled;
                        this.bump = (VersionBump)EditorGUILayout.EnumPopup(this.bump, StylesUtils.CenteredPopupStyle, sideButtonOptions);
                    }

                    using (HorizontalScope.Enter())
                    {
                        //Output folder field
                        EditorGUIUtility.labelWidth = 110f;
                        EditorGUILayout.PropertyField(this.outputFolder, outputDirectoryLabel, outputFolderOptions);
                        //Browse button
                        if (GUILayout.Button("Browse...", EditorStyles.miniButton, sideButtonOptions))
                        {
                            //Get relative path to project folder
                            string relative = BuildToolUtils.OpenProjectFolderPanel("Select build folder");
                            //If the path has changed, apply it
                            if (this.window.Settings.OutputFolder != relative)
                            {
                                this.outputFolder.stringValue = relative;
                            }
                        }
                    }

                    //Target flags selection
                    this.targetFlags.intValue = (int)(BuildTargetFlags)EditorGUILayout.EnumFlagsField(buildTargetsLabel, (BuildTargetFlags)this.targetFlags.intValue, targetsOptions);
                    EditorGUIUtility.labelWidth = 0f;
                    EditorGUILayout.Space(30f);
                    //Build button
                    GUI.backgroundColor = StylesUtils.Green;
                    if (GUILayout.Button(buildButton, StylesUtils.BuildButtonStyle, buildButtonOptions))
                    {
                        if ((BuildTargetFlags)this.targetFlags.intValue == BuildTargetFlags.None)
                        {
                            EditorUtility.DisplayDialog("Error", "Please select at least one target to build for", "Okay");
                        }
                        else if (this.window.Settings.PublishRelease && string.IsNullOrEmpty(this.title))
                        {
                            EditorUtility.DisplayDialog("Error", "Your GitHub release requires a title", "Okay");
                        }
                        else
                        {
                            //Confirm build
                            string targets = string.Join("\n", BuildToolUtils.GetTargets((BuildTargetFlags)this.targetFlags.intValue));
                            if (EditorUtility.DisplayDialog("Building Game", "Building for the following targets:\n" + targets, "Build", "Cancel"))
                            {
                                this.window.StartBuild();
                                return;
                            }
                        }
                    }
                    GUI.backgroundColor = Color.white;
                }

                using (VerticalScope.Enter())
                {
                    //Copy files and folders panel header
                    EditorGUILayout.LabelField(copyLabel, StylesUtils.CenteredLabelStyle);
                    //List scrollview
                    using (ScrollViewScope.Enter(ref this.scroll, false, false, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, EditorStyles.helpBox, filesScrollOptions))
                    {
                        int index = 0, toDelete = -1;
                        //List all folders and files to copy
                        foreach (SerializedProperty toCopy in this.copyOnBuild)
                        {
                            using (HorizontalScope.Enter())
                            {
                                //Delete entry button
                                GUI.backgroundColor = StylesUtils.Red;
                                if (GUILayout.Button(deleteEntry, StylesUtils.DeleteButtonStyle, deleteButtonOptions))
                                {
                                    //Store index to delete it later (else it breaks the enumeration
                                    toDelete = index;
                                }

                                GUI.backgroundColor = Color.white;
                                //Entry to copy label
                                toCopy.NextVisible(true); //Path
                                string path = toCopy.stringValue;
                                //Make sure the path isn't too long to be displayed
                                if (path.Length > 40)
                                {
                                    path = path.Substring(0, 37) + "...";
                                }

                                EditorGUILayout.LabelField(new GUIContent(path, toCopy.stringValue));
                                toCopy.NextVisible(true); //Location
                                toCopy.intValue = (int)(BuildItem.CopyLocation)EditorGUILayout.EnumPopup((BuildItem.CopyLocation)toCopy.intValue, copyPopupOptions);
                            }

                            index++;
                        }

                        //If an entry has been marked to delete, delete it now
                        if (toDelete != -1)
                        {
                            this.copyOnBuild.DeleteArrayElementAtIndex(toDelete);
                        }
                    }

                    EditorGUILayout.Space(5f);
                    using (HorizontalScope.Enter())
                    {
                        //Add file browser
                        if (GUILayout.Button("Add file...", EditorStyles.miniButton, addButtonOptions))
                        {
                            //Get relative path
                            string relative = BuildToolUtils.OpenProjectFilePanel("Select file to copy on build");
                            //If a valid file is selected, add it
                            if (!string.IsNullOrEmpty(relative) && !this.copyOnBuild.Children().Any(p => p.Contains(BuildItem.PATH_NAME, relative)))
                            {
                                //Increment size and set at the end
                                this.copyOnBuild.arraySize++;
                                SerializedProperty item = this.copyOnBuild.GetArrayElementAtIndex(this.copyOnBuild.arraySize - 1);
                                item.NextVisible(true); //Path
                                item.stringValue = relative;
                            }
                        }

                        GUILayout.Space(20f);
                        if (GUILayout.Button("Add folder...", EditorStyles.miniButton, addButtonOptions))
                        {
                            //Get relative path
                            string relative = BuildToolUtils.OpenProjectFolderPanel("Select folder to copy on build");
                            //If a valid folder is selected, add it
                            if (!string.IsNullOrEmpty(relative) && !this.copyOnBuild.Children().Any(p => p.Contains(BuildItem.PATH_NAME, relative)))
                            {
                                //Increment size and set at the end
                                this.copyOnBuild.arraySize++;
                                SerializedProperty item = this.copyOnBuild.GetArrayElementAtIndex(this.copyOnBuild.arraySize - 1);
                                item.NextVisible(true); //Path
                                item.stringValue = relative;
                            }
                        }
                    }
                }
            }
            EditorGUILayout.Space(15f);
        }
        #endregion
    }
}