#if !DEBUG
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BuildTool.Extensions;
using BuildTool.UI;
using Octokit;
using UnityEditor;
using UnityEngine;

namespace BuildTool
{
    /// <summary>
    /// GitHub Authenticator utility to create and manage Authorization Tokens
    /// </summary>
    public sealed class GitHubAuthenticator
    {
        /// <summary>
        /// Indicates the connection status of the GitHub client
        /// </summary>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public enum ConnectionStatus
        {
            NONE,
            NOT_CONNECTED,
            BAD_CREDENTIALS,
            REQUIRES_2FA,
            FAILED_2FA,
            CONNECTED
        }

        /// <summary>
        /// EqualityComparer used to store Users in a Dictionary
        /// </summary>
        private class UserEqualityComparer : IEqualityComparer<User>
        {
            #region Instance
            /// <summary>
            /// Comparer instance
            /// </summary>
            public static UserEqualityComparer Comparer { get; } = new UserEqualityComparer();
            #endregion

            #region Constructors
            /// <summary>
            /// Prevents instantiation, user <see cref="Comparer"/> instead
            /// </summary>
            private UserEqualityComparer() { }
            #endregion

            #region Methods
            /// <inheritdoc/>
            public bool Equals(User a, User b) => string.Equals(a?.Login, b?.Login);

            /// <inheritdoc/>
            public int GetHashCode(User user) => user.Login.GetHashCode();
            #endregion
        }

        #region Constants
        /// <summary>
        /// Name of the applet
        /// </summary>
        private const string appName = "unity-build-tool";
        /// <summary>
        /// Name of the stored credentials file
        /// </summary>
        private const string fileName = "ubt.bin";
        /// <summary>
        /// Format of the saved date time
        /// </summary>
        private const string timeFormat = "dd/MM/yy-HH:mm:ss";
        /// <summary>
        /// GitHub permission scopes
        /// </summary>
        private static readonly string[] scopes = { "repo", "read:user", "user:email" };
        /// <summary>
        /// User repositories requests settings
        /// </summary>
        private static readonly RepositoryRequest request = new RepositoryRequest
        {
            Direction = SortDirection.Ascending,
            Sort      = RepositorySort.FullName,
            Type      = RepositoryType.All
        };
        /// <summary>
        /// Path to the credentials file location on the disk
        /// </summary>
        private static readonly string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UnityBuildTool", fileName);
        /// <summary>
        /// Assembly version of the BuildTool
        /// </summary>
        private static readonly string assemblyVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;
        /// <summary>
        /// Unique Entropy used to encrypt the token to the disk
        /// </summary>
        private static readonly byte[] entropy;

        #endregion

        #region Fields
        private readonly GitHubClient client;
        private readonly BuildToolWindow window;
        private bool setup;
        #endregion

        #region Static properties
        /// <summary>
        /// Github authentication token, reads and writes to the encrypted file on the disk
        /// </summary>
        private static string Token
        {
            get => Encoding.ASCII.GetString(ProtectedData.Unprotect(File.ReadAllBytes(filePath), entropy, DataProtectionScope.CurrentUser));
            set
            {
                //Make sure the folder exists
                Directory.GetParent(filePath).Create();
                //Save the token
                File.WriteAllBytes(filePath, ProtectedData.Protect(Encoding.ASCII.GetBytes(value), entropy, DataProtectionScope.CurrentUser));
            }
        }

        /// <summary>
        /// Creates a new application authorization to the GitHub API
        /// </summary>
        private static NewAuthorization Authorization => new NewAuthorization($"{appName}-{Environment.MachineName}-{DateTime.Now.ToString(timeFormat, CultureInfo.InvariantCulture)}", scopes);
        #endregion

        #region Properties
        /// <summary>
        /// If the client is connected or not
        /// </summary>
        public bool IsConnected => this.Status == ConnectionStatus.CONNECTED;

        private ConnectionStatus status;
        /// <summary>
        /// Connection status of the Client
        /// </summary>
        public ConnectionStatus Status
        {
            get => this.status;
            private set
            {
                this.status = value;
                this.window.UIEnabled = true;
                this.window.Repaint();
            }
        }

        /// <summary>
        /// Currently connected user
        /// </summary>
        public User User { get; private set; }

        /// <summary>
        /// The primary email address of the User
        /// </summary>
        public string Email { get; private set; }

        /// <summary>
        /// The best known name of this user
        /// </summary>
        private string Name => string.IsNullOrEmpty(this.User.Name) ? this.User.Login : this.User.Name;

        /// <summary>
        /// Repositories this user has access to
        /// This object is thread-safe
        /// </summary>
        public RepositorySelector Selector { get; private set; }

        /// <summary>
        /// If the repositories the user has access to have been fetched yet or not
        /// </summary>
        public bool RepositoriesFetched { get; private set; }

        /// <summary>
        /// The Repository to use for builds
        /// </summary>
        public Repository CurrentRepository { get; private set; }

        /// <summary>
        /// If the branches for the current repository are currently being fetched
        /// </summary>
        public bool FetchingBranches { get; private set; }

        /// <summary>
        /// Branch/head commit list for the current repository
        /// </summary>
        public ReadOnlyCollection<(Branch branch, GitHubCommit commit)> CurrentBranches { get; private set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Does one time initialization for Authenticator objects
        /// </summary>
        static GitHubAuthenticator()
        {
            //Remove all non-hex characters
            string id = Regex.Replace(SystemInfo.deviceUniqueIdentifier.ToLowerInvariant(), "[^a-f0-9]", string.Empty);
            //Convert hex pairs to bytes
            entropy = Enumerable.Range(0, id.Length / 2).Select(i => Convert.ToByte(id.Substring(i * 2, 2), 16)).ToArray();
        }

        /// <summary>
        /// Creates a new GitHubAuthenticator but does not connect to the API, use <see cref="Connect"/>
        /// </summary>
        /// <param name="window">Window this Authenticator is associated to</param>
        public GitHubAuthenticator(BuildToolWindow window)
        {
            //Creates a new GitHub client
            this.client = new GitHubClient(new ProductHeaderValue(appName, assemblyVersion));
            this.window = window;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Tries to connect to the API using the stored credentials file
        /// Can only be called once
        /// </summary>
        public async void Connect()
        {
            //Only run this once
            if (this.setup) { return; }
            this.setup = true;

            //Tries to load the user's credentials
            if (File.Exists(filePath))
            {
                this.Log("Loading user credentials...\n");
                try
                {
                    //Load token and tries to connect
                    this.client.Credentials = new Credentials(Token);
                    TestConnection();

                    //Fetch all repositories the user has access to
                    await FetchAllRepositories();
                    return;
                }
                catch
                {
                    //If any error whatsoever happens, assume the credentials are bad or nonexistent and ask them from the user
                    this.LogWarning("Could not get valid credentials from disk");
                    //Delete existing credentials if they are on the disk
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        this.Log("Deleting credentials file from disk");
                    }
                }
            }
            //If nothing is found, request credentials from user
            else { this.Log("No credentials stored on disk, requesting token"); }
            //Set status to request credentials
            this.Status = ConnectionStatus.NOT_CONNECTED;
        }

        /// <summary>
        /// Submits the username and password credentials and attempts to login to the GitHub account
        /// </summary>
        /// <param name="username">Connection username</param>
        /// <param name="password">Connection password</param>
        public void SubmitCredentials(string username, string password)
        {
            //Create Username/Password credentials and attempt to request an access token
            this.client.Credentials = new Credentials(username, password);
            RequestToken();
        }

        /// <summary>
        /// Submits the Two Factor Authorization code and attempts to login
        /// </summary>
        /// <param name="twoFactorCode">Two Factor Authorization code</param>
        public void Submit2FA(string twoFactorCode) => RequestToken(twoFactorCode);

        /// <summary>
        /// Requests an Authorization Token from Github
        /// </summary>
        /// <param name="twoFactorCode">Two Factor Authorization code to obtain authorization code</param>
        private async void RequestToken(string twoFactorCode = null)
        {
            //Store current credentials in case the new ones are invalid
            Credentials previousCredentials = this.client.Credentials;
            try
            {
                //Tries to get an Authorization Token, submitting the 2FA code if provided
                ApplicationAuthorization auth = await (string.IsNullOrEmpty(twoFactorCode) ? this.client.Authorization.Create(Authorization) : this.client.Authorization.Create(Authorization, twoFactorCode));

                //Submit and test connection
                this.client.Credentials = new Credentials(auth.Token);
                TestConnection();

                //Save and encrypt the token
                Token = auth.Token;

                //Fetch all repositories the user has access to
                await FetchAllRepositories();
            }
            catch (TwoFactorRequiredException)
            {
                //2FA Code required
                if (string.IsNullOrEmpty(twoFactorCode))
                {
                    this.LogWarning("Login requires 2FA code");
                    this.Status = ConnectionStatus.REQUIRES_2FA;
                }
                //2FA Code invalid
                else
                {
                    this.LogWarning("Two Factor Code invalid");
                    this.Status = ConnectionStatus.FAILED_2FA;
                }
                this.client.Credentials = previousCredentials;
            }
            catch (AuthorizationException)
            {
                //Invalid token/username-password combo
                this.LogError("Invalid credentials");
                this.Status = ConnectionStatus.BAD_CREDENTIALS;
                this.client.Credentials = previousCredentials;
            }
        }

        /// <summary>
        /// Tests the validity of the connection to GitHub
        /// </summary>
        private async void TestConnection()
        {
            //Get current user and email to see if the connection worked
            this.User = await this.client.User.Current();
            this.Email = (await this.client.User.Email.GetAll()).First(e => e.Primary).Email;

            //Notify connection
            this.Log($"Connected to user {this.User.Login} ({this.Email})");
            this.Status = ConnectionStatus.CONNECTED;
        }

        /// <summary>
        /// Asynchronously fetches all the repositories a User has access to and stores them in the object
        /// This method can be called from a different thread
        /// </summary>
        private async Task FetchAllRepositories()
        {
            //Request user repositories
            IReadOnlyList<Repository> userRepositories = await this.client.Repository.GetAllForCurrent(request);

            //Get all accessible repositories for this user, store in repo owner/repos structure
            Dictionary<User, List<RepositoryInfo>> repoOwners = new Dictionary<User, List<RepositoryInfo>>(UserEqualityComparer.Comparer);
            foreach (Repository repository in userRepositories.Where(r => r.Permissions.Push && r.Permissions.Pull))
            {
                //Create new repo info
                RepositoryInfo newRepo = new RepositoryInfo(repository, this.window);
                //Check if the owner already has a list
                if (repoOwners.TryGetValue(repository.Owner, out List<RepositoryInfo> repos))
                {
                    //If so add it
                    repos.Add(newRepo);
                }
                else
                {
                    //Else create the list and add it to the dictionary
                    repos = new List<RepositoryInfo> { newRepo };
                    repoOwners.Add(repository.Owner, repos);
                }
            }

            //Create owner UI objects from previous data
            List<RepositoryOwner> owners = new List<RepositoryOwner>(repoOwners.Count);
            foreach (KeyValuePair<User, List<RepositoryInfo>>  owner  in repoOwners)
            {
                owners.Add(new RepositoryOwner(owner.Key, owner.Value, this.window));
            }
            //Sort them for display purpose
            owners.Sort();

            //Create selector and get selected repository if any
            this.Selector = new RepositorySelector(owners, this);
            SetBuildRepository();

            //Set fetch flag and repaint the UI
            this.RepositoriesFetched = true;
            this.window.Repaint();

            //Print the information fetched
            this.Log($"{this.Selector.TotalRepos} repositories fetched for {this.User.Login}");
        }

        /// <summary>
        /// Sets the currently selected repository as the build repository
        /// </summary>
        public async void SetBuildRepository()
        {
            //Sets the repository if it isn't null
            if (this.Selector.SelectedRepository != null)
            {
                //Block UI while we work
                this.FetchingBranches = true;

                //Set new repository
                this.CurrentRepository = this.Selector.SelectedRepository;
                this.window.SerializedSettings.FindProperty(BuildToolSettings.BUILD_REPOSITORY_NAME).stringValue = this.CurrentRepository.FullName;

                //Get branches and commits
                IReadOnlyList<Branch> branches = await this.client.Repository.Branch.GetAll(this.CurrentRepository.Id);

                //Get the latest commit for each branch
                List<(Branch, GitHubCommit)> heads = new List<(Branch, GitHubCommit)>(branches.Count);
                foreach (Branch branch in branches)
                {
                    //Save as a branch/commit tuple
                    GitHubCommit commit = await this.client.Repository.Commit.Get(this.CurrentRepository.Id, branch.Commit.Sha);
                    //Check if this is the default branch
                    if (branch.Name == this.CurrentRepository.DefaultBranch)
                    {
                        //If it is, put it in front
                        heads.Insert(0, (branch, commit));
                    }
                    //Else put it at the end
                    else { heads.Add((branch, commit)); }
                }

                //Set branches list
                this.CurrentBranches = heads.AsReadOnly();
                this.Log($"{this.CurrentBranches.Count} branches found for repository {this.CurrentRepository.FullName}");

                //Display UI again.
                this.FetchingBranches = false;
            }
        }

        /// <summary>
        /// Creates a new release with the specified information
        /// </summary>
        /// <param name="version">Version object to bump for the release</param>
        /// <param name="targets">BuildTargets to release for</param>
        /// <param name="snapshot">Snapshot of the release info to create the release from</param>
        /// <param name="token">Token to cancel this task</param>
        public async Task CreateNewRelease(BuildVersion version, BuildTarget[] targets, BuildHandler.ReleaseSnapshot snapshot, CancellationToken token)
        {
            //Set progressbar stuff
            this.window.progressTitle = "GitHub Release";
            this.window.status = "Creating Tag";
            this.window.total = targets.Length + 2;
            this.window.current = 0;

            //Create new Tag
            NewTag newTag = new NewTag
            {
                Message = snapshot.title,
                Tag = version.VersionString,
                Object = snapshot.targetSHA,
                Type = TaggedType.Commit,
                Tagger = new Committer(this.Name, this.Email, version.BuildTime)
            };
            //Send tag to GitHub
            GitTag tag = await this.client.Git.Tag.Create(this.CurrentRepository.Id, newTag);
            //Check for cancellation
            token.ThrowIfCancellationRequested();

            //Progressbar update
            this.window.status = "Creating release";
            this.window.current++;

            //Create new release
            NewRelease newRelease = new NewRelease(tag.Tag)
            {
                Name = snapshot.title,
                Body = snapshot.description,
                TargetCommitish = snapshot.targetSHA,
                Prerelease = snapshot.prerelease,
                Draft = snapshot.draft
            };
            //Send new release
            Release release = await this.client.Repository.Release.Create(this.CurrentRepository.Id, newRelease);
            this.Log("Release created: " + release.Name);
            //Check for cancellation
            token.ThrowIfCancellationRequested();
            //Progressbar update
            this.window.current++;

            //Upload all zip files
            string buildsFolder = Path.Combine(BuildToolUtils.ProjectFolderPath, this.window.Settings.OutputFolder);
            foreach (BuildTarget target in targets)
            {
                //Progressbar update
                #if !DEBUG
                string fileName = $"{this.window.productName}_{BuildToolUtils.GetBuildTargetName(target)}{version.VersionString}.zip";
                #endif
                this.window.status = "Uploading asset " + fileName;

                string path = Path.Combine(buildsFolder, fileName);
                using (FileStream zipStream = File.OpenRead(path))
                {
                    await this.client.Repository.Release.UploadAsset(release, new ReleaseAssetUpload(fileName, "application/zip", zipStream, null));
                }
                this.Log("Uploaded asset: " + path);

                //Check for cancellation
                token.ThrowIfCancellationRequested();
                //Progressbar update
                this.window.current++;
            }

            //Final log
            this.Log("Release creation complete");
        }
        #endregion
    }
}
#endif