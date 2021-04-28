using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Octokit;
using UnityBuildTool.DeviceFlow;
using UnityBuildTool.Extensions;
using UnityBuildTool.UI;
using UnityEditor;
using UnityEngine;

//ReSharper disable InconsistentNaming

namespace UnityBuildTool
{
    /// <summary>
    /// GitHub Authenticator utility to create and manage Authorization Tokens
    /// </summary>
    public sealed class GitHubAuthenticator : ICredentialStore
    {
        /// <summary>
        /// Indicates the connection status of the GitHub client
        /// </summary>
        public enum ConnectionStatus
        {
            NONE,
            NOT_CONNECTED,
            BAD_TOKEN,
            AWAITING_VERIFICATION,
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
        /// <summary>Name of the applet</summary>
        private const string appName  = "unity-build-tool";
        /// <summary>Application version</summary>
        private const string appVersion  = "0.2.0.0";
        /// <summary>Application client ID</summary>
        private const string clientID = "907b67dfbc8ba4f5af77";
        /// <summary>Name of the stored credentials file</summary>
        private const string fileName = "ubt.bin";
        /// <summary>GitHub permission scopes</summary>
        private static readonly string[] scopes = { "repo", "read:user", "user:email" };
        /// <summary>User repositories requests settings</summary>
        private static readonly RepositoryRequest repositoryRequest = new RepositoryRequest
        {
            Direction = SortDirection.Ascending,
            Sort      = RepositorySort.FullName,
            Type      = RepositoryType.All
        };
        /// <summary>Unique Entropy used to encrypt the token to the disk</summary>
        private static readonly byte[] entropy;

        /// <summary>
        /// Credentials folder location on disk
        /// </summary>
        public static string CredentialsFolder { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UnityBuildTool");
        /// <summary>
        /// Path to the credentials file location on the disk
        /// </summary>
        public static string CredentialsFilePath { get; } = Path.Combine(CredentialsFolder, fileName);
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
            get => Encoding.ASCII.GetString(ProtectedData.Unprotect(File.ReadAllBytes(CredentialsFilePath), entropy, DataProtectionScope.CurrentUser));
            set
            {
                //Make sure the folder exists
                Directory.GetParent(CredentialsFilePath)?.Create();
                //Save the token
                File.WriteAllBytes(CredentialsFilePath, ProtectedData.Protect(Encoding.ASCII.GetBytes(value), entropy, DataProtectionScope.CurrentUser));
            }
        }
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
                this.window.MustRepaint = true;
            }
        }

        /// <summary>
        /// OAuth verification user code
        /// </summary>
        public string UserCode { get; private set; }

        /// <summary>
        /// OAuth verification URL
        /// </summary>
        public string VerificationURL { get; private set; }

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
            this.client = new GitHubClient(new ProductHeaderValue(appName, appVersion), this);
            this.window = window;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Gets the credentials from the file on disk
        /// </summary>
        /// <returns>A task to retrieve the credentials</returns>
        public Task<Credentials> GetCredentials() => Task.FromResult(new Credentials(Token));

        /// <summary>
        /// Tries to connect to the API using the stored credentials file
        /// Can only be called once
        /// </summary>
        public async void Connect()
        {
            //Only run this once
            if (this.setup) return;

            this.setup = true;
            //Tries to load the user's credentials
            if (File.Exists(CredentialsFilePath))
            {
                this.Log("Loading user credentials...\n");
                try
                {
                    //Test connection for errors
                    await TestConnection();
                    //Fetch all repositories the user has access to
                    await FetchAllRepositories();
                    return;
                }
                catch
                {
                    //If any error whatsoever happens, assume the credentials are bad or nonexistent and ask them from the user
                    this.LogWarning("Could not get valid credentials from disk");
                    //Delete existing credentials if they are on the disk
                    if (File.Exists(CredentialsFilePath))
                    {
                        File.Delete(CredentialsFilePath);
                        this.Log("Deleting credentials file from disk");
                    }
                }
            }
            else
            {
                //If nothing is found, request credentials from user
                this.Log("No credentials stored on disk, requesting token");
            }

            //Set status to request credentials
            this.Status = ConnectionStatus.NOT_CONNECTED;
        }

        /// <summary>
        /// Starts the device flow OAuth process
        /// </summary>
        public async void StartDeviceFlow() => await DeviceFlowAuth().ConfigureAwait(false);

        /// <summary>
        /// Device Flow OAuth task
        /// </summary>
        /// <returns>The authentication task</returns>
        private async Task DeviceFlowAuth()
        {
            try
            {
                //Request a user code first
                OAuthDeviceFlowRequest request = new OAuthDeviceFlowRequest(clientID, scopes);
                OAuthDeviceFlowResponse response = await this.client.Oauth.InitiateDeviceFlow(request).ConfigureAwait(false);
                //Store user code and verification URL
                this.UserCode = response.userCode;
                this.VerificationURL = response.verificationUrl;

                //Start verification process
                this.Status = ConnectionStatus.AWAITING_VERIFICATION;
                OAuthDeviceFlowTokenRequest tokenRequest = new OAuthDeviceFlowTokenRequest(clientID, response.deviceCode)
                {
                    expiry = DateTime.Now + TimeSpan.FromSeconds(response.expiry),
                    pollRate = TimeSpan.FromSeconds(response.pollRate)
                };
                OAuthDeviceFlowTokenResponse tokenResponse = await this.client.Oauth.PollDeviceFlowAccessTokenResult(tokenRequest).ConfigureAwait(false);

                //Make sure the token is valid
                if (string.IsNullOrEmpty(tokenResponse.accessToken))
                {
                    this.Status = ConnectionStatus.BAD_TOKEN;
                    return;
                }

                //Store token and clear from memory
                Token = tokenResponse.accessToken;
                //ReSharper disable once RedundantAssignment
                tokenResponse = null;

                //Test connection to GitHub
                await TestConnection();
                //Fetch all if successfully connected
                await FetchAllRepositories();

                //Clear UserCode and Verification URL
                this.UserCode = null;
                this.VerificationURL = null;
            }
            catch (Exception e)
            {
                this.window.UIEnabled = true;
                this.LogException(e);
                this.Status = ConnectionStatus.BAD_TOKEN;

                //Delete existing credentials if they are on the disk
                if (File.Exists(CredentialsFilePath))
                {
                    File.Delete(CredentialsFilePath);
                    this.Log("Deleting credentials file from disk");
                }
            }
        }

        /// <summary>
        /// Tests the validity of the connection to GitHub
        /// </summary>
        private async Task TestConnection()
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
            IReadOnlyList<Repository> userRepositories = await this.client.Repository.GetAllForCurrent(repositoryRequest);

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
            owners.AddRange(repoOwners.Select(owner => new RepositoryOwner(owner.Key, owner.Value, this.window)));
            //Sort them for display purpose
            owners.Sort();

            //Create selector and get selected repository if any
            this.Selector = new RepositorySelector(owners, this);
            SetBuildRepository();

            //Set fetch flag and repaint the UI
            this.RepositoriesFetched = true;

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
                    else
                    {
                        //Else put it at the end
                        heads.Add((branch, commit));
                    }
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
            try
            {
                //Send tag to GitHub
                await this.client.Git.Tag.Create(this.CurrentRepository.Id, newTag);
            }
            catch (ApiException e)
            {
                //We get an error if it already existed
                this.Log($"Tag {newTag.Tag} already exists");
                this.LogError(e);
            }
            //Check for cancellation
            token.ThrowIfCancellationRequested();

            //Progressbar update
            this.window.status = "Creating release";
            this.window.current++;

            //Create new release
            NewRelease newRelease = new NewRelease(newTag.Tag)
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
                string currentFileName = $"{this.window.productName}_{BuildToolUtils.GetBuildTargetName(target)}{version.VersionString}.zip";
                this.window.status = "Uploading asset " + currentFileName;

                string path = Path.Combine(buildsFolder, currentFileName);
                using (FileStream zipStream = File.OpenRead(path))
                {
                    await this.client.Repository.Release.UploadAsset(release, new ReleaseAssetUpload(currentFileName, "application/zip", zipStream, null), token);
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