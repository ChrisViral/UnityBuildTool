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

        //Options
        private static readonly GUILayoutOption[] sectionOptions       = { GUILayout.Width(325f) };
        private static readonly GUILayoutOption[] requestButtonOptions = { GUILayout.Height(40f) };
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
            EditorGUILayout.Space();
            using (HorizontalScope.Enter(sectionOptions))
            {
                EditorGUILayout.Space();
                using (VerticalScope.Enter())
                {
                    //Connection UI
                    if (!this.window.Authenticator.IsConnected)
                    {
                        //Repaint when the status changes
                        if (this.window.Authenticator.Status != this.previousStatus)
                        {
                            this.previousStatus = this.window.Authenticator.Status;
                            this.window.Repaint();
                        }

                        //Status label
                        EditorGUILayout.LabelField(status[this.window.Authenticator.Status], StylesUtils.CenteredBoldLabel);
                        EditorGUILayout.Space();

                        //UI depends on connection state
                        switch (this.window.Authenticator.Status)
                        {
                            case ConnectionStatus.NOT_CONNECTED:
                            case ConnectionStatus.BAD_TOKEN:
                                if (GUILayout.Button("Request Verification Code", StylesUtils.ConnectionButtonStyle, requestButtonOptions))
                                {
                                    this.window.Authenticator.StartDeviceFlow();
                                    this.window.UIEnabled = false;
                                }
                                break;

                            case ConnectionStatus.AWAITING_VERIFICATION:
                                EditorGUILayout.SelectableLabel(this.window.Authenticator.UserCode, StylesUtils.UserCodeLabel);
                                EditorGUILayout.Space();
                                if (GUILayout.Button("Open GitHub", StylesUtils.ConnectionButtonStyle, requestButtonOptions))
                                {
                                    Process.Start(this.window.Authenticator.VerificationURL);
                                }
                                break;
                        }
                    }
                    else
                    {
                        //Connected message
                        EditorGUILayout.LabelField("You are connected as", this.window.Authenticator.User.Login, EditorStyles.boldLabel);
                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField("Connected to the GitHub API, please wait...", StylesUtils.CenteredBoldLabel);
                    }
                }
            }
        }
        #endregion
    }
}