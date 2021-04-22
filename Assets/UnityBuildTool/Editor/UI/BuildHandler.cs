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
            //Fade group
            this.urlUsed.target = EditorGUILayout.ToggleLeft(useURL, this.urlUsed.target);
            this.window.SerializedSettings.FindProperty(BuildToolSettings.USE_WEB_SERVICE_NAME).boolValue = this.urlUsed.target;
            if (!string.IsNullOrEmpty(this.apiURL) && !this.urlUsed.target)
            {
                this.window.SerializedSettings.FindProperty(BuildToolSettings.VERSION_URL_NAME).stringValue = string.Empty;
                this.apiURL = string.Empty;
            }

            using (FadeGroupScope.Enter(this.urlUsed.faded, out bool visible))
            {
                if (visible)
                {
                    //Get URL
                    EditorGUI.indentLevel++;
                    EditorGUIUtility.labelWidth = 95f;
                    this.apiURL = EditorGUILayout.TextField(url, this.apiURL, GUILayout.Width(426f));
                    EditorGUIUtility.labelWidth = 0f;
                    using (HorizontalScope.Enter())
                    {
                        //Display coloured connection label
                        (string label, GUIStyle style) = StylesUtils.ConnectionStyles[this.window.APIStatus];
                        EditorGUILayout.LabelField(label, style, GUILayout.Width(91f));
                        //Disable connection when the URL hasn't changed
                        GUI.enabled = this.window.UIEnabled && !string.IsNullOrEmpty(this.apiURL) && this.apiURL != this.window.Settings.VersionURL;
                        if (GUILayout.Button(connect, GUILayout.Width(331f)))
                        {
                            //Set on the serialized property
                            this.window.SerializedSettings.FindProperty(BuildToolSettings.VERSION_URL_NAME).stringValue = this.apiURL;
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
                GUIContent label = new GUIContent(branchesNotLoaded ? "Waiting for repository data to be loaded..." : "Waiting for valid BuildVersion from web API...");
                float width = EditorStyles.boldLabel.CalcSize(label).x;
                EditorGUILayout.LabelField(label, EditorStyles.boldLabel, GUILayout.Width(width));
                //Repaint to make sure the change appears as soon as possible
                this.window.Repaint();
                return;
            }

            //Make sure the branch data is loaded
            EnsureBranchesLoaded();

            //Display the release UI
            GUI.enabled = this.window.UIEnabled && this.window.Settings.PublishRelease;

            //Header
            EditorGUILayout.LabelField(header, StylesUtils.TitleLabelStyle, GUILayout.Height(30f), GUILayout.Width(150f));
            EditorGUILayout.Space();
            //Title field
            this.title = EditorGUILayout.TextField(string.Empty, this.title, StylesUtils.TitleFieldStyle, GUILayout.Height(25f));
            EditorGUILayout.Space(EditorGUIUtility.standardVerticalSpacing * 2f);
            using (HorizontalScope.Enter())
            {

                GUI.enabled = this.window.UIEnabled && this.window.Settings.PublishRelease;
                //Branch selector
                EditorGUIUtility.labelWidth = 45f;
                this.selectedBranch = EditorGUILayout.Popup(branchSelector, this.selectedBranch, this.branches, GUILayout.Width(175f));
                //Repository label
                EditorGUIUtility.labelWidth = 15f;
                EditorGUILayout.LabelField("in", this.window.Authenticator.CurrentRepository.FullName, EditorStyles.boldLabel);
            }

            EditorGUIUtility.labelWidth = 80f;
            //Last commit on branch label
            EditorGUILayout.LabelField("Last commit:", $"[{this.CurrentCommit.Sha.Substring(0, 7)}] {this.CurrentCommit.Commit.Message}");
            EditorGUIUtility.labelWidth = 0f;
            EditorGUILayout.Space();
            //Release description
            EditorGUILayout.LabelField(descriptionLabel, StylesUtils.DescriptionLabelStyle, GUILayout.Width(100f), GUILayout.Height(30f));
            this.description = EditorGUILayout.TextArea(this.description, StylesUtils.DescriptionFieldStyle, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

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
                    SerializedProperty prop = this.window.SerializedSettings.FindProperty(BuildToolSettings.PUBLISH_RELEASE_NAME);
                    prop.boolValue = EditorGUILayout.ToggleLeft(publishToggle, prop.boolValue);
                    //Dev build toggle
                    prop = this.window.SerializedSettings.FindProperty(BuildToolSettings.DEVELOPMENT_BUILD_NAME);
                    prop.boolValue = EditorGUILayout.ToggleLeft(devBuildToggle, prop.boolValue);
                    //Version bump settings
                    using (HorizontalScope.Enter())
                    {
                        EditorGUILayout.LabelField(bumpLabel, GUILayout.Width(109f));
                        GUI.enabled = false;
                        EditorGUILayout.TextField(this.window.BuildVersion.GetBumpedVersionString(this.bump), GUILayout.Width(188f));
                        GUI.enabled = this.window.UIEnabled;
                        this.bump = (VersionBump)EditorGUILayout.EnumPopup(this.bump, StylesUtils.CenteredPopupStyle, GUILayout.Width(70f));
                    }

                    using (HorizontalScope.Enter())
                    {
                        //Output folder property
                        prop = this.window.SerializedSettings.FindProperty(BuildToolSettings.OUTPUT_FOLDER_NAME);
                        //Output folder field
                        EditorGUIUtility.labelWidth = 110f;
                        EditorGUILayout.PropertyField(prop, outputDirectoryLabel, GUILayout.Width(300f));
                        //Browse button
                        if (GUILayout.Button("Browse...", EditorStyles.miniButton, GUILayout.Width(70f)))
                        {
                            //Get relative path to project folder
                            string relative = BuildToolUtils.OpenProjectFolderPanel("Select build folder");
                            //If the path has changed, apply it
                            if (this.window.Settings.OutputFolder != relative)
                            {
                                prop.stringValue = relative;
                            }
                        }
                    }

                    //Target flags property
                    prop = this.window.SerializedSettings.FindProperty(BuildToolSettings.TARGET_FLAGS_NAME);
                    //Target flags selection
                    prop.intValue = (int)(BuildTargetFlags)EditorGUILayout.EnumFlagsField(buildTargetsLabel, (BuildTargetFlags)prop.intValue, GUILayout.Width(375f));
                    EditorGUIUtility.labelWidth = 0f;
                    EditorGUILayout.Space(30f);
                    //Build button
                    GUI.backgroundColor = StylesUtils.Green;
                    if (GUILayout.Button(buildButton, StylesUtils.BuildButtonStyle, GUILayout.Height(60f), GUILayout.Width(375f)))
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
                }

                using (VerticalScope.Enter())
                {
                    //Copy files and folders panel header
                    EditorGUILayout.LabelField(copyLabel, StylesUtils.CenteredLabelStyle);
                    SerializedProperty prop = this.window.SerializedSettings.FindProperty(BuildToolSettings.COPY_ON_BUILD_NAME);
                    //List scrollview
                    using (ScrollViewScope.Enter(ref this.scroll, false, false, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, EditorStyles.helpBox, GUILayout.Height(200f), GUILayout.MinWidth(400f)))
                    {
                        int index = 0, toDelete = -1;
                        //List all folders and files to copy
                        foreach (SerializedProperty toCopy in prop)
                        {
                            using (HorizontalScope.Enter())
                            {
                                //Delete entry button
                                GUI.backgroundColor = StylesUtils.Red;
                                if (GUILayout.Button(deleteEntry, StylesUtils.DeleteButtonStyle, GUILayout.Width(20f)))
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
                                toCopy.intValue = (int)(BuildItem.CopyLocation)EditorGUILayout.EnumPopup((BuildItem.CopyLocation)toCopy.intValue, GUILayout.Width(100f));
                            }

                            index++;
                        }

                        //If an entry has been marked to delete, delete it now
                        if (toDelete != -1)
                        {
                            prop.DeleteArrayElementAtIndex(toDelete);
                        }
                    }

                    EditorGUILayout.Space(5f);
                    using (HorizontalScope.Enter())
                    {
                        //Add file browser
                        if (GUILayout.Button("Add file...", EditorStyles.miniButton))
                        {
                            //Get relative path
                            string relative = BuildToolUtils.OpenProjectFilePanel("Select file to copy on build");
                            //If a valid file is selected, add it
                            if (!string.IsNullOrEmpty(relative) && !prop.Children().Any(p => p.Contains(BuildItem.PATH_NAME, relative)))
                            {
                                //Increment size and set at the end
                                prop.arraySize++;
                                SerializedProperty item = prop.GetArrayElementAtIndex(prop.arraySize - 1);
                                item.NextVisible(true); //Path
                                item.stringValue = relative;
                            }
                        }

                        EditorGUILayout.Space(10f);
                        if (GUILayout.Button("Add folder...", EditorStyles.miniButton))
                        {
                            //Get relative path
                            string relative = BuildToolUtils.OpenProjectFolderPanel("Select folder to copy on build");
                            //If a valid folder is selected, add it
                            if (!string.IsNullOrEmpty(relative) && !prop.Children().Any(p => p.Contains(BuildItem.PATH_NAME, relative)))
                            {
                                //Increment size and set at the end
                                prop.arraySize++;
                                SerializedProperty item = prop.GetArrayElementAtIndex(prop.arraySize - 1);
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