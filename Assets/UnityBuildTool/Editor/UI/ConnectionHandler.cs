using System.Collections.Generic;
using System.Diagnostics;
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
        /// <summary>Status message dictionary</summary>
        private static readonly Dictionary<ConnectionStatus, string> status = new Dictionary<ConnectionStatus, string>(6)
        {
            [ConnectionStatus.NONE]                  = "Connecting to API, please wait...",
            [ConnectionStatus.NOT_CONNECTED]         = "Please connect to your GitHub account",
            [ConnectionStatus.BAD_TOKEN]             = "Authentication failed, please retry connection",
            [ConnectionStatus.AWAITING_VERIFICATION] = "Please proceed to GitHub and enter the following verification code",
            [ConnectionStatus.CONNECTED]             = string.Empty
        };
        #endregion

        #region Fields
        private readonly BuildToolWindow window;
        private ConnectionStatus previousStatus;
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
                if (this.window.Authenticator.Status != this.previousStatus)
                {
                    this.previousStatus = this.window.Authenticator.Status;
                    this.window.Repaint();
                }

                //Status label
                EditorGUILayout.LabelField(status[this.window.Authenticator.Status], EditorStyles.boldLabel);
                EditorGUILayout.Space();

                //UI depends on connection state
                switch (this.window.Authenticator.Status)
                {
                    case ConnectionStatus.NOT_CONNECTED:
                    case ConnectionStatus.BAD_TOKEN:
                        if (GUILayout.Button("Request Verification Code"))
                        {
                            this.window.Authenticator.StartDeviceFlow();
                        }
                        break;

                    case ConnectionStatus.AWAITING_VERIFICATION:
                        EditorGUILayout.SelectableLabel(this.window.Authenticator.UserCode);
                        if (GUILayout.Button("Open GitHub"))
                        {
                            Process.Start(this.window.Authenticator.VerificationURL);
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