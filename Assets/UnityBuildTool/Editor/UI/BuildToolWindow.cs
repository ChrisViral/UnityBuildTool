using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityBuildTool.Extensions;
using UnityEditor;
using UnityEngine;

namespace UnityBuildTool.UI
{
    /// <summary>
    /// Build Tool editor window
    /// </summary>
    public class BuildToolWindow : EditorWindow
    {
        /// <summary>
        /// The status of the connection to the webservice providing build info
        /// </summary>
        public enum BuildAPIStatus
        {
            NOT_CONNECTED = 0,
            ERROR         = 1,
            CONNECTED     = 2
        }

        #region Constants
        /// <summary>Window title</summary>
        private const string WINDOW_TITLE = "Build Tool";

        //GUI content
        private static readonly GUIContent refreshButton = new GUIContent("Refresh", "Refresh the connection to GitHub and get any changes");

        //Options
        private static readonly GUILayoutOption[] selectorOptions      = { GUILayout.Width(450f) };
        private static readonly GUILayoutOption[] refreshButtonOptions = { GUILayout.Width(200f), GUILayout.Height(40f) };
        #endregion

        #region Static Properties
        /// <summary>
        /// Current active BuildToolWindow
        /// </summary>
        public static BuildToolWindow Window { get; private set; }

        /// <summary>
        /// Path to the BuildFile on the disk
        /// </summary>
        public static string BuildFilePath { get; private set; }
        #endregion

        #region Fields
        //GUI objects
        private ConnectionHandler connectionHandler;
        private BuildHandler buildHandler;
        private CancellationTokenSource cancellationSource;
        private BuildHandler.ReleaseSnapshot snapshot;
        private Task buildTask;
        private BuildTarget originalTarget;
        private BuildTarget[] targets;
        internal string productName;
        internal string progressTitle, status;
        internal int current;
        internal float total = 1f; //Prevents div0 errors
        private bool clear, unfocus;
        private string popupTitle, popupContent;
        #endregion

        #region Properties
        /// <summary>
        /// The GitHubAuthenticator connected to this BuildToolWindow
        /// </summary>
        public GitHubAuthenticator Authenticator { get; private set; }

        [SerializeField]
        private BuildToolSettings settings;
        /// <summary>
        /// The Settings for this BuildTool
        /// </summary>
        public BuildToolSettings Settings => this.settings;

        /// <summary>
        /// The SerializedObject associated to the Settings object
        /// </summary>
        public SerializedObject SerializedSettings { get; private set; }

        /// <summary>
        /// If the UI is currently enabled
        /// </summary>
        public bool UIEnabled { get; set; }

        /// <summary>
        /// If the window should be repainted
        /// </summary>
        public bool MustRepaint { get; set; }

        /// <summary>
        /// Current BuildVersion
        /// </summary>
        public BuildVersion BuildVersion { get; private set; }

        /// <summary>
        /// Checks if the build task exists and is currently running
        /// </summary>
        private bool BuildTaskRunning => this.buildTask != null && !this.buildTask.IsCanceled && !this.buildTask.IsFaulted && !this.buildTask.IsCompleted;

        private BuildAPIStatus apiStatus = BuildAPIStatus.NOT_CONNECTED;
        /// <summary>
        /// Status of the connection to the build webservice
        /// </summary>
        public BuildAPIStatus APIStatus
        {
            get => this.settings.UseWebService ? this.apiStatus : BuildAPIStatus.NOT_CONNECTED;
            private set => this.apiStatus = value;
        }

        /// <summary>
        /// If a valid API connection has been made
        /// </summary>
        public bool APIConnected => this.APIStatus == BuildAPIStatus.CONNECTED && !string.IsNullOrEmpty(this.settings.VersionURL);
        #endregion

        #region Static methods
        /// <summary>
        /// Allows to show the window from the top menu
        /// </summary>
        [MenuItem("Tools/Build Tool")]
        private static void ShowWindow()
        {
            //Check if a window exists
            if (HasOpenInstances<BuildToolWindow>())
            {
                //If so focus it
                FocusWindowIfItsOpen<BuildToolWindow>();
            }
            else
            {
                //Else create a new one
                Window = CreateWindow<BuildToolWindow>(WINDOW_TITLE, typeof(SceneView));
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Initializes the window
        /// </summary>
        private void Init()
        {
            //Load the settings file
            BuildFilePath = Path.Combine(Directory.GetParent(BuildToolUtils.DataPath)?.FullName ?? string.Empty, BuildToolUtils.ProductName.ToLowerInvariant() + BuildVersion.EXTENSION);
            this.settings = BuildToolSettings.Load();

            //Load all secondary objects
            RefreshConnection();

        }

        /// <summary>
        /// Sets up the connection to GitHub and all the necessary objects
        /// </summary>
        public void RefreshConnection()
        {
            //Create SerializedObject for the current settings
            this.SerializedSettings = new SerializedObject(this.Settings);

            //Initialize connection
            this.Authenticator = new GitHubAuthenticator(this);
            //Create UI objects
            this.connectionHandler = new ConnectionHandler(this);
            this.buildHandler = new BuildHandler(this);

            //Connect to GitHub
            this.Authenticator.Connect();
        }

        /// <summary>
        /// Gets the latest build from the web service
        /// </summary>
        public async void GetBuild()
        {
            //Turn off the UI
            this.UIEnabled = false;
            //Make sure we have the latest settings
            this.SerializedSettings.ApplyModifiedPropertiesWithoutUndo();
            this.Log("Attempting to get build from " + this.Settings.VersionURL);

            //Try to get the build from the webservice
            try
            {
                this.BuildVersion = await BuildVersionWebClient.GetBuildVersion(this.Settings.VersionURL);
            }
            catch (Exception e)
            {
                this.LogException(e);
            }
            if (this.BuildVersion != null)
            {
                this.APIStatus = BuildAPIStatus.CONNECTED;
                this.Log("Reply:\n" + this.BuildVersion.InfoString());
            }
            else
            {
                this.APIStatus = BuildAPIStatus.ERROR;
                this.LogError("Build version could not be fetched successfully, getting from file");
                GetBuildFromFile();
            }
            //Repaint and reenable the UI
            Repaint();
            this.UIEnabled = true;
        }

        /// <summary>
        /// Resets the current API Status to not connected
        /// </summary>
        public void ClearBuild() => this.APIStatus = BuildAPIStatus.NOT_CONNECTED;

        /// <summary>
        /// Fetches the BuildVersion from the text file
        /// </summary>
        public void GetBuildFromFile() => this.BuildVersion = BuildVersion.FromFile();

        /// <summary>
        /// Starts the building process asynchronously
        /// </summary>
        public void StartBuild()
        {
            //Disable UI
            this.UIEnabled = false;

            //Bump up BuildVersion
            this.snapshot = this.buildHandler;
            this.BuildVersion.Build(this.snapshot.bump, this.Authenticator.User.Login, this.APIStatus == BuildAPIStatus.CONNECTED ? this.settings.VersionURL : null);

            //Build all then reset progressbar
            this.targets = BuildToolUtils.GetTargets(this.settings.TargetFlags).ToArray();
            Builder.BuildAll(this.Settings, this.targets, out bool[] success, false);
            this.originalTarget = EditorUserBuildSettings.activeBuildTarget;

            //Finish the build process asynchronously
            this.cancellationSource = new CancellationTokenSource();
            this.productName = BuildToolUtils.ProductName;
            this.buildTask = PostBuildAsync(success, this.cancellationSource.Token);
            this.buildTask.ContinueWith(OnBuildProcessComplete);
            Repaint();
        }

        /// <summary>
        /// Copies necessary and creates compressed zip folder for each build target
        /// </summary>
        /// <param name="success">Array containing the fail/pass status of each target build</param>
        /// <param name="token">CancellationToken to cancel the ongoing task</param>
        private async Task PostBuildAsync(IReadOnlyList<bool> success, CancellationToken token)
        {
            //Get the builds folder path
            string buildsFolder = Path.Combine(BuildToolUtils.ProjectFolderPath, this.Settings.OutputFolder);
            //Get progressbar stuff
            int index = this.current = 0;
            int steps = 2 + this.settings.CopyOnBuild.Count;
            this.total = this.targets.Length * steps;
            //Check for cancellation
            token.ThrowIfCancellationRequested();
            //Loop through all targets
            foreach (BuildTarget target in this.targets)
            {
                //Get target name
                string targetName = BuildToolUtils.GetBuildTargetName(target);

                //If the build has failed, ignore this target
                if (!success[index])
                {
                    this.current += steps;
                    this.Log($"Skipping post-build on {targetName} because the build did not succeed");
                    continue;
                }

                //Get output folder
                string outputDirectory = Path.Combine(buildsFolder, targetName);
                string buildAppData = Path.Combine(outputDirectory, BuildToolUtils.GetAppDataPath(target, this.productName));
                this.progressTitle = targetName + " Post Build";
                try
                {
                    //Copy the build file over
                    this.status = targetName + " - Copying build file";
                    await BuildToolUtils.CopyFileAsync(BuildFilePath, Path.Combine(buildAppData, this.productName.ToLowerInvariant() + BuildVersion.EXTENSION));
                    this.Log($"Copied build file to {targetName} build folder");
                }
                catch (Exception e)
                {
                    //Log exceptions
                    Debug.LogException(e);
                    Debug.LogError($"[Builder]: Could not copy build file to build folder on platform {targetName}, please check output folder");
                }
                //Increment progressbar (copying build file)
                this.current++;
                //Check for cancellation
                token.ThrowIfCancellationRequested();

                //Loop through all files and folders to copy
                foreach (BuildItem toCopy in this.Settings.CopyOnBuild)
                {
                    //Get the path to that file or folder
                    string path = Path.Combine(BuildToolUtils.ProjectFolderPath, toCopy.Path);
                    string copyToPath = string.Empty;
                    switch (toCopy.Location)
                    {
                        case BuildItem.CopyLocation.Root:
                            copyToPath = outputDirectory;
                            break;
                        case BuildItem.CopyLocation.InAppData:
                            copyToPath = buildAppData;
                            break;
                        case BuildItem.CopyLocation.WithAppData:
                            copyToPath = Directory.GetParent(buildAppData)?.FullName ?? string.Empty;
                            break;
                    }
                    try
                    {
                        //File to copy
                        if (File.Exists(path))
                        {
                            //Copy the file over
                            this.status = targetName + " - Copying file " + toCopy;
                            await BuildToolUtils.CopyFileAsync(path, Path.Combine(copyToPath, Path.GetFileName(path)));
                            this.Log($"Copied file {path} to {targetName} build folder");
                        }
                        //Folder to copy
                        else if (Directory.Exists(path))
                        {
                            //ReSharper disable once AssignNullToNotNullAttribute <- we know it won't be because of the prior Exists call
                            //Copy the folder over
                            this.status = targetName + " - Copying folder " + toCopy;
                            await BuildToolUtils.CopyFolderAsync(path, Path.Combine(copyToPath, Path.GetFileName(path)));
                            this.Log($"Copied folder {path} to {targetName} build folder");
                        }
                        //Neither, invalid
                        else { Debug.LogWarning("Invalid file or folder: " + path); }
                    }
                    catch (Exception e)
                    {
                        //Log exceptions
                        Debug.LogException(e);
                        Debug.LogError($"[Builder]: Could not copy additional file {path} to build folder on platform {targetName}, please check output folder");
                    }
                    //Increment progressbar (copying additional files)
                    this.current++;
                    //Check for cancellation
                    token.ThrowIfCancellationRequested();
                }

                //Delete any previous zip files for this target group
                this.status = targetName + " - Creating build zip";
                foreach (string toDelete in Directory.EnumerateFiles(buildsFolder, $"{this.productName}_{targetName}v*.zip", SearchOption.TopDirectoryOnly))
                {
                    await Task.Run(() => File.Delete(toDelete), token).ConfigureAwait(false);
                    this.Log("Deleted old zip file " + toDelete);
                }

                //Get the path to the new zip file
                string zipPath = Path.Combine(buildsFolder, $"{this.productName}_{targetName}{this.BuildVersion.VersionString}.zip");
                try
                {
                    //Create the zip file
                    await BuildToolUtils.CreateZipAsync(zipPath, Path.Combine(buildsFolder, targetName));
                    this.Log("Created new zip file " + zipPath);
                }
                catch (Exception e)
                {
                    //Log exceptions
                    Debug.LogException(e);
                    Debug.LogError($"[Builder]: Could not create output zip {zipPath} on platform {targetName}, please check output folder");
                }
                //Increment progressbar (creating zip file)
                this.current++;
                //Check for cancellation
                token.ThrowIfCancellationRequested();
                index++;
            }

            if (this.settings.PublishRelease)
            {
                //Finish by creating GitHub release
                await this.Authenticator.CreateNewRelease(this.BuildVersion, this.targets, this.snapshot, token);
            }
        }

        /// <summary>
        /// Ran when the build task finishes
        /// </summary>
        /// <param name="task">Task that initiated the callback</param>
        private void OnBuildProcessComplete(Task task)
        {
            //Log the result
            if (task.IsCanceled)
            {
                this.Log("Build canceled");
                this.popupTitle = "Build Cancelled";
                this.popupContent = "Build process cancelled";
            }
            else if (task.IsFaulted)
            {
                if (task.Exception != null)
                {
                    this.LogException(task.Exception);
                }
                this.Log("Build encountered a problem and shutdown");
                this.popupTitle = "Build Failed";
                this.popupContent = "The build has failed\nCheck log for more info";

            }
            else
            {
                this.Log("Build successfully completed");
                this.popupTitle = "Build Succeeded";
                this.popupContent = "Build completed successfully!";
            }
            //Clear the task stuff
            if (this.cancellationSource != null)
            {
                this.cancellationSource.Dispose();
                this.cancellationSource = null;
            }
            this.buildTask = null;

            //Reset the UI and reenable
            this.buildHandler.Reset();
            this.UIEnabled = this.clear = this.unfocus = true;
            Repaint();
        }
        #endregion

        #region Functions
        private void Awake()
        {
            //If a window already exists, do not open a new one
            if (Window && Window != this)
            {
                Destroy(this);
            }
        }

        private void OnEnable()
        {
            //If a window already exists, do not open a new one
            if (Window && Window != this)
            {
                Destroy(this);
            }
            else
            {
                //Set the window as this and init the object
                Window = this;
                Init();
            }
        }

        private void OnGUI()
        {
            //Check if the object has just been deserialized and needs a reconnection
            if (this.SerializedSettings is null)
            {
                Init();
            }

            //UI state
            GUI.enabled = this.UIEnabled;

            //Clear focus
            if (this.unfocus)
            {
                GUI.FocusControl(null);
                this.unfocus = false;
            }

            //Check if we are currently running the build task
            if (this.BuildTaskRunning)
            {
                //Display progressbar
                if (EditorUtility.DisplayCancelableProgressBar(this.progressTitle, this.status, this.current / this.total))
                {
                    //Cancel task and clear progressbar
                    this.cancellationSource.Cancel();
                }
                //Always repaint when running the task
                Repaint();
            }

            //If the repositories list hasn't been loaded yet, display connection UI
            if (!this.Authenticator.RepositoriesFetched)
            {
                this.connectionHandler.OnGUI();
            }
            else
            {
                using (HorizontalScope.Enter())
                {
                    using (VerticalScope.Enter(selectorOptions))
                    {
                        EditorGUILayout.Space();
                        //API URL selection
                        this.buildHandler.URLSelector();
                        EditorGUILayout.Space();

                        //Repository selection GUI
                        this.Authenticator.Selector.OnGUI();

                        //Refresh button
                        GUILayout.FlexibleSpace();
                        using (HorizontalScope.Enter())
                        {
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button(refreshButton, StylesUtils.RefreshButtonStyle, refreshButtonOptions))
                            {
                                RefreshConnection();
                                return;
                            }
                            GUILayout.FlexibleSpace();
                        }
                        EditorGUILayout.Space(10f);
                    }

                    using (VerticalScope.Enter())
                    {
                        //Release creator
                        this.buildHandler.ReleaseCreator();
                    }

                    GUILayout.FlexibleSpace();
                }
            }

            //Apply properties
            if (this.SerializedSettings.hasModifiedProperties)
            {
                this.SerializedSettings.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(this.SerializedSettings.targetObject);
            }

            //Restore UI state
            GUI.enabled = true;
        }

        private void OnInspectorUpdate()
        {
            //If the clear flag is on (post-build)
            if (this.clear)
            {
                //Clear the ProgressBar
                EditorUtility.ClearProgressBar();
                Repaint();
                //Reset the BuildTarget
                if (EditorUserBuildSettings.activeBuildTarget != this.originalTarget)
                {
                    EditorUserBuildSettings.SwitchActiveBuildTargetAsync(BuildPipeline.GetBuildTargetGroup(this.originalTarget), this.originalTarget);
                }
                //Display success popup
                EditorUtility.DisplayDialog(this.popupTitle, this.popupContent, "Okay");
                //Reset clear flag
                this.clear = false;
            }

            //Check if a repaint has been requested
            if (this.MustRepaint)
            {
                this.MustRepaint = false;
                Repaint();
            }
        }
        #endregion
    }
}