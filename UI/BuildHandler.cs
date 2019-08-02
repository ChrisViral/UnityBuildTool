using System.Collections.Generic;
using System.Linq;
using BuildTool.Extensions;
using Octokit;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;
using VersionBump = BuildTool.BuildVersion.VersionBump;
using BuildAPIStatus = BuildTool.UI.BuildToolWindow.BuildAPIStatus;

namespace BuildTool.UI
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
            /// <summary>
            /// The VersionBump to apply
            /// </summary>
            public readonly VersionBump bump;
            /// <summary>
            /// The title of the release
            /// </summary>
            public readonly string title;
            /// <summary>
            /// The description of the release
            /// </summary>
            public readonly string description;
            /// <summary>
            /// The target commit's SHA
            /// </summary>
            public readonly string targetSHA;
            /// <summary>
            /// If this is a prerelease
            /// </summary>
            public readonly bool prerelease;
            /// <summary>
            /// If the release should be saved as a draft
            /// </summary>
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
        /// <summary>
        /// Array containing the GUIStyles of labels indicating the connection of the webservice
        /// </summary>
        private static readonly Dictionary<BuildAPIStatus, (string, GUIStyle)> connectionStyles = new Dictionary<BuildAPIStatus, (string, GUIStyle)>(3);

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
        private static readonly GUIContent bumpLabel            = new GUIContent("Bump",                  "How to bump the version number");
        private static readonly GUIContent outputDirectoryLabel = new GUIContent("Output Directory:",     "Local path to the directory where the output of the builds should be saved");
        private static readonly GUIContent buildTargetsLabel    = new GUIContent("Build targets:",        "Which platforms the game will be built for");
        private static readonly GUIContent buildButton          = new GUIContent("BUILD",                 "Build the game for the selected targets and release to GitHub");
        private static readonly GUIContent copyLabel            = new GUIContent("Files/Folders to copy", "Files and folders that should be copied over when building the game");
        private static readonly GUIContent deleteEntry          = new GUIContent("X",                     "Delete this entry to copy on build");
        #endregion

        #region Static fields
        //GUI Styles
        private static bool initStyles;
        private static GUIStyle titleStyle, titleFieldStyle, descriptionLabelStyle, descriptionFieldStyle, centeredLabelStyle, centeredPopupStyle, deleteButtonStyle, buildButtonStyle;
        #endregion

        #region Fields
        //Objects
        private readonly BuildToolWindow window;

        //GUI Fields
        private readonly AnimBool urlUsed = new AnimBool(false);
        private string title, description, apiURL;
        private string[] branches;
        private int selectedBranch;
        private VersionBump bump;
        private bool prerelease, draft;
        private Vector2 scroll;
        #endregion

        #region Properties
        /// <summary>
        /// The last commit of the currently selected branch
        /// </summary>
        private GitHubCommit CurrentCommit => this.window.Authenticator.CurrentBranches[this.selectedBranch].commit;
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

            //Try to get the build if there is a valid URL
            if (!string.IsNullOrEmpty(this.apiURL)) { this.window.GetBuild(); }
            //Else get it from the file
            else { this.window.GetBuildFromFile(); }
        }
        #endregion

        #region Static methods
        /// <summary>
        /// Initializes various used GUIStyles
        /// </summary>
        private static void InitStyles()
        {
            if (!initStyles)
            {
                //Make sure we only init once
                initStyles = true;

                //Connection label styles
                connectionStyles.Add(BuildAPIStatus.NOT_CONNECTED, ("Not Connected", EditorStyles.boldLabel));
                connectionStyles.Add(BuildAPIStatus.ERROR,         ("Error",     new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = BuildToolUtils.Red } }));
                connectionStyles.Add(BuildAPIStatus.CONNECTED,     ("Connected", new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = BuildToolUtils.Green } }));

                //Release styles
                titleStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 20,
                    alignment = TextAnchor.MiddleLeft
                };
                titleFieldStyle = new GUIStyle(EditorStyles.textField)
                {
                    fontSize = 16,
                    alignment = TextAnchor.MiddleLeft,
                    fontStyle = FontStyle.Bold
                };
                descriptionLabelStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14,
                    alignment = TextAnchor.MiddleLeft

                };
                descriptionFieldStyle = new GUIStyle(EditorStyles.textArea) { fontSize = 13 };
                centeredLabelStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };
                centeredPopupStyle = new GUIStyle(EditorStyles.popup) { alignment = TextAnchor.MiddleCenter };
                deleteButtonStyle = new GUIStyle(EditorStyles.miniButton)
                {
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white },
                    active = { textColor = Color.white }
                };
                buildButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    fontSize = 24,
                    normal = { textColor = Color.white },
                    active = { textColor = Color.white }
                };
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
            }
        }

        /// <summary>
        /// Resets the release creation window to default
        /// </summary>
        public void Reset()
        {
            this.title = this.description = string.Empty;
            this.selectedBranch = 0;
            this.bump = VersionBump.Revision;
            this.prerelease = this.draft = false;
        }

        /// <summary>
        /// Displays this control
        /// </summary>
        public void URLSelector()
        {
            //Make sure styles are initiated
            InitStyles();

            //Fade group
            this.urlUsed.target = EditorGUILayout.ToggleLeft(useURL, this.urlUsed.target);
            this.window.SerializedSettings.FindProperty(BuildToolSettings.USE_WEB_SERVICE_NAME).boolValue = this.urlUsed.target;
            if (EditorGUILayout.BeginFadeGroup(this.urlUsed.faded))
            {
                //Get URL
                EditorGUIUtility.labelWidth = 105f;
                this.apiURL = EditorGUILayout.TextField(url, this.apiURL);
                EditorGUIUtility.labelWidth = 0f;
                EditorGUILayout.BeginHorizontal();

                //Display coloured connection label
                (string label, GUIStyle style) = connectionStyles[this.window.APIStatus];
                EditorGUILayout.LabelField(label, style, GUILayout.Width(101f));
                //Disable connection when the URL hasn't changed
                GUI.enabled = !string.IsNullOrEmpty(this.apiURL) && this.apiURL != this.window.Settings.VersionURL && this.window.UIEnabled;
                if (GUILayout.Button(connect))
                {
                    //Set on the serialized property
                    this.window.SerializedSettings.FindProperty(BuildToolSettings.VERSION_URL_NAME).stringValue = this.apiURL;
                    //Try to get the build
                    this.Log("Testing connection to " + this.apiURL);
                    this.window.GetBuild();
                }
                GUI.enabled = this.window.UIEnabled;
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndFadeGroup();
        }

        /// <summary>
        /// Displays the Release creator UI
        /// </summary>
        public void ReleaseCreator()
        {
            //Make sure styles are initiated
            InitStyles();

            //Only display UI if the branches have been loaded and the build version exists
            bool branchesNotLoaded = this.window.Authenticator.FetchingBranches || this.window.Authenticator.CurrentBranches is null;
            if (branchesNotLoaded || this.window.BuildVersion is null)
            {
                //Set the local branches to null to make sure the array is reinitialized when they have been fetched/repo has been changed
                this.branches = null;
                //Display info label
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(branchesNotLoaded ? "Waiting for repository data to be loaded..." : "Waiting for valid BuildVersion from web API...", EditorStyles.boldLabel);
                //Repaint to make sure the change appears as soon as possible
                this.window.Repaint();
                return;
            }

            //Make sure the branch data is loaded
            EnsureBranchesLoaded();

            //Display the release UI
            GUI.enabled = this.window.UIEnabled && this.window.Settings.PublishRelease;
            EditorGUILayout.BeginVertical();
            //Header
            EditorGUILayout.LabelField(header, titleStyle, GUILayout.Height(30f), GUILayout.Width(150f));
            EditorGUILayout.Space();
            //Title field
            this.title = EditorGUILayout.TextField(string.Empty, this.title, titleFieldStyle, GUILayout.Height(25f));
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = this.window.UIEnabled && this.window.Settings.PublishRelease;
            //Branch selector
            EditorGUIUtility.labelWidth = 50f;
            this.selectedBranch = EditorGUILayout.Popup(branchSelector, this.selectedBranch, this.branches, GUILayout.Width(175f));
            //Repository label
            EditorGUIUtility.labelWidth = 15f;
            EditorGUILayout.LabelField("in", this.window.Authenticator.CurrentRepository.FullName, EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            EditorGUIUtility.labelWidth = 80f;
            //Last commit on branch label
            EditorGUILayout.LabelField("Last commit:", $"[{this.CurrentCommit.Sha.Substring(0, 7)}] {this.CurrentCommit.Commit.Message}");
            EditorGUIUtility.labelWidth = 0f;
            EditorGUILayout.Space();
            //Release description
            EditorGUILayout.LabelField(descriptionLabel, descriptionLabelStyle, GUILayout.Width(100f), GUILayout.Height(30f));
            this.description = EditorGUILayout.TextArea(this.description, descriptionFieldStyle, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

            //Display bottom Build UI
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            //Prerelease/draft toggles
            this.prerelease = EditorGUILayout.ToggleLeft(prereleaseToggle, this.prerelease);
            this.draft = EditorGUILayout.ToggleLeft(draftToggle, this.draft);
            GUI.enabled = this.window.UIEnabled;
            //Publish toggle
            SerializedProperty prop = this.window.SerializedSettings.FindProperty(BuildToolSettings.PUBLISH_RELEASE_NAME);
            prop.boolValue = EditorGUILayout.ToggleLeft(publishToggle, prop.boolValue);
            //Dev build toggle
            prop = this.window.SerializedSettings.FindProperty(BuildToolSettings.DEVELOPMENT_BUILD_NAME);
            prop.boolValue = EditorGUILayout.ToggleLeft(devBuildToggle, prop.boolValue);
            //Version bump settings
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 110f;
            GUI.enabled = false;
            EditorGUILayout.TextField(bumpLabel, this.window.BuildVersion.GetBumpedVersionString(this.bump), GUILayout.Width(300f));
            GUI.enabled = this.window.UIEnabled;
            this.bump = (VersionBump)EditorGUILayout.EnumPopup(this.bump, centeredPopupStyle, GUILayout.Width(70f));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            //Output folder property
            prop = this.window.SerializedSettings.FindProperty(BuildToolSettings.OUTPUT_FOLDER_NAME);
            //Output folder field
            EditorGUILayout.PropertyField(prop, outputDirectoryLabel, GUILayout.Width(300f));
            //Browse button
            if (GUILayout.Button("Browse...", EditorStyles.miniButton, GUILayout.Width(70f)))
            {
                //Get relative path to project folder
                string relative = BuildToolUtils.OpenProjectFolderPanel("Select build folder");
                //If the path has changed, apply it
                if (this.window.Settings.OutputFolder != relative) { prop.stringValue = relative; }
            }
            EditorGUILayout.EndHorizontal();
            //Target flags property
            prop = this.window.SerializedSettings.FindProperty(BuildToolSettings.TARGET_FLAGS_NAME);
            //Target flags selection
            prop.intValue = (int)(BuildTargetFlags)EditorGUILayout.EnumFlagsField(buildTargetsLabel, (BuildTargetFlags)prop.intValue, GUILayout.Width(375f));
            EditorGUIUtility.labelWidth = 0f;
            GUILayout.Space(40f);
            //Build button
            GUI.backgroundColor = BuildToolUtils.Green;
            if (GUILayout.Button(buildButton, buildButtonStyle, GUILayout.Height(60f), GUILayout.Width(375f)))
            {
                if ((BuildTargetFlags)prop.intValue == BuildTargetFlags.None)
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
                    string targets = string.Join("\n", BuildToolUtils.GetTargets((BuildTargetFlags)prop.intValue));
                    if (EditorUtility.DisplayDialog("Building Game", "Building for the following targets:\n" + targets, "Build", "Cancel"))
                    {
                        this.window.StartBuild();
                        return;
                    }
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            //Copy files and folders panel header
            EditorGUILayout.LabelField(copyLabel, centeredLabelStyle);
            prop = this.window.SerializedSettings.FindProperty(BuildToolSettings.COPY_ON_BUILD_NAME);
            //List scrollview
            this.scroll = EditorGUILayout.BeginScrollView(this.scroll, false, false, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, BuildToolUtils.BackgroundStyle, GUILayout.Height(200f), GUILayout.MinWidth(400f));
            int index = 0, toDelete = -1;
            //List all folders and files to copy
            foreach (SerializedProperty toCopy in prop)
            {
                EditorGUILayout.BeginHorizontal();
                //Delete entry button
                GUI.backgroundColor = BuildToolUtils.Red;
                if (GUILayout.Button(deleteEntry, deleteButtonStyle, GUILayout.Width(20f)))
                {
                    //Store index to delete it later (else it breaks the enumeration
                    toDelete = index;
                }
                GUI.backgroundColor = Color.white;
                //Entry to copy label
                EditorGUILayout.LabelField(toCopy.stringValue);
                EditorGUILayout.EndHorizontal();
                index++;
            }
            //If an entry has been marked to delete, delete it now
            if (toDelete != -1) { prop.DeleteArrayElementAtIndex(toDelete); }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(4f);
            //Add file browser
            if (GUILayout.Button("Add file...", EditorStyles.miniButton))
            {
                //Get relative path
                string relative = BuildToolUtils.OpenProjectFilePanel("Select file to copy on build");
                //If a valid file is selected, add it
                if (!string.IsNullOrEmpty(relative) && !BuildToolUtils.PropertyContains(prop, relative))
                {
                    //Increment size and set at the end
                    prop.arraySize++;
                    prop.GetArrayElementAtIndex(prop.arraySize - 1).stringValue = relative;
                }
            }
            GUILayout.Space(10f);
            if (GUILayout.Button("Add folder...", EditorStyles.miniButton))
            {
                //Get relative path
                string relative = BuildToolUtils.OpenProjectFolderPanel("Select folder to copy on build");
                //If a valid folder is selected, add it
                if (!string.IsNullOrEmpty(relative) && !BuildToolUtils.PropertyContains(prop, relative))
                {
                    //Increment size and set at the end
                    prop.arraySize++;
                    prop.GetArrayElementAtIndex(prop.arraySize - 1).stringValue = relative;
                }
            }
            GUILayout.Space(12f);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10f);
            EditorGUILayout.EndVertical();
        }
        #endregion
    }
}
