using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using ConnectionStatus = UnityBuildTool.GitHubAuthenticator.ConnectionStatus;

namespace UnityBuildTool.UI
{
    /// <summary>
    /// A GitHub connection handler UI Control
    /// </summary>
    public class ConnectionHandler
    {
        #region Constants
        /// <summary>2FA Code number only filter</summary>
        private static readonly Regex numberFilter = new Regex("[^0-9]", RegexOptions.Compiled);
        /// <summary>Status message dictionary</summary>
        private static readonly Dictionary<ConnectionStatus, string> status = new Dictionary<ConnectionStatus, string>(6)
        {
            [ConnectionStatus.NONE]            = "Connecting to API, please wait...",
            [ConnectionStatus.NOT_CONNECTED]   = "Please enter your GitHub credentials",
            [ConnectionStatus.BAD_CREDENTIALS] = "Invalid credentials, please try again",
            [ConnectionStatus.REQUIRES_2FA]    = "2FA Code required",
            [ConnectionStatus.FAILED_2FA]      = "2FA verification failed, please try again",
            [ConnectionStatus.CONNECTED]       = string.Empty
        };

        //GUIContent labels
        private static readonly GUIContent username = new GUIContent("Username: ", "Your GitHub username");
        private static readonly GUIContent password = new GUIContent("Password: ", "Your GitHub password");
        private static readonly GUIContent code     = new GUIContent("2FA Code: ", "Enter the 2FA code from your authenticator app");
        #endregion

        #region Fields
        private readonly BuildToolWindow window;
        private string user = string.Empty, pass = string.Empty, authCode = string.Empty;
        private bool mustFocus = true;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new ConnectionHandler control for the given BuildToolWindow
        /// </summary>
        /// <param name="window">The window to attach this handler to</param>
        public ConnectionHandler(BuildToolWindow window) => this.window = window;
        #endregion

        #region Methods
        /// <summary>
        /// Displays this ConnectionHandler control
        /// </summary>
        public void OnGUI()
        {
            //Connection UI
            if (!this.window.Authenticator.IsConnected)
            {
                //Enter key capture
                bool enterPressed = Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return;

                //Status label
                EditorGUILayout.LabelField(status[this.window.Authenticator.Status], EditorStyles.boldLabel);
                EditorGUILayout.Space();

                //UI depends on connection state
                switch (this.window.Authenticator.Status)
                {
                    case ConnectionStatus.NOT_CONNECTED:
                    case ConnectionStatus.BAD_CREDENTIALS:

                        //Get the username and password
                        EditorGUIUtility.labelWidth = 70f;
                        GUI.SetNextControlName(nameof(this.user));
                        this.user = EditorGUILayout.TextField(username, this.user, GUILayout.Width(250f));
                        GUI.SetNextControlName(nameof(this.pass));
                        this.pass = EditorGUILayout.PasswordField(password, this.pass, GUILayout.Width(250f));
                        EditorGUIUtility.labelWidth = 0f;

                        //Focus on controls
                        if (this.mustFocus && this.window.UIEnabled)
                        {
                            GUI.FocusControl(this.window.Authenticator.Status == ConnectionStatus.BAD_CREDENTIALS ?  nameof(this.pass) : nameof(this.user));
                            this.mustFocus = false;
                        }

                        //Attempt login
                        using (new EditorGUILayout.HorizontalScope(GUILayout.Width(250f)))
                        {
                            GUILayout.Space(70f);
                            EditorGUILayout.Space();
                            if (GUILayout.Button("Login") || enterPressed)
                            {
                                //Submit credentials
                                this.window.Authenticator.SubmitCredentials(this.user, this.pass);
                                this.mustFocus = true;

                                //Disable the UI while we wait
                                this.window.UIEnabled = false;
                            }
                            EditorGUILayout.Space();
                        }

                        break;

                    case ConnectionStatus.REQUIRES_2FA:
                    case ConnectionStatus.FAILED_2FA:

                        //This cancels focus text selection
                        Color c = GUI.skin.settings.cursorColor;
                        GUI.skin.settings.cursorColor = Color.clear;

                        //2FA code entry
                        EditorGUIUtility.labelWidth = 70f;
                        GUI.SetNextControlName(nameof(this.authCode));
                        this.authCode = EditorGUILayout.TextField(code, this.authCode, GUILayout.Width(250f));
                        GUI.skin.settings.cursorColor = c;
                        EditorGUIUtility.labelWidth = 0f;

                        //Allows editing the text by code to control the content/Initial focus of the control
                        if (this.mustFocus && this.window.UIEnabled)
                        {
                            GUI.FocusControl(nameof(this.authCode));
                            this.mustFocus = false;
                        }
                        else
                        {
                            //Remove all letters and extra characters
                            int len = this.authCode.Length;
                            this.authCode = numberFilter.Replace(this.authCode.Trim(), string.Empty);
                            if (this.authCode.Length > 6)
                            {
                                this.authCode = this.authCode.Substring(0, 6);
                            }

                            //If changes occured, un-focus to apply them
                            if (this.authCode.Length != len)
                            {
                                GUI.FocusControl(null);
                                this.mustFocus = true;
                            }
                        }

                        //Submit 2FA code
                        using (new EditorGUILayout.HorizontalScope(GUILayout.Width(250f)))
                        {
                            GUILayout.Space(70f);
                            EditorGUILayout.Space();
                            if (GUILayout.Button("Submit") || enterPressed)
                            {
                                //Submit 2FA code
                                this.window.Authenticator.Submit2FA(this.authCode);
                                this.mustFocus = true;

                                //Disable the UI while we wait
                                this.window.UIEnabled = false;
                            }
                            EditorGUILayout.Space();
                        }
                        break;
                }
            }
            else
            {
                //Connected message
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("You are connected as", this.window.Authenticator.User.Login, EditorStyles.boldLabel);
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Connecting to the GitHub API, please wait...", EditorStyles.boldLabel);
            }
        }
        #endregion
    }
}