﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BuildTool.Extensions;
using Octokit;
using UnityEditor;
using UnityEngine;

namespace BuildTool.UI
{
    /// <summary>
    /// Repository selector UI Control
    /// </summary>
    public class RepositorySelector
    {
        #region Constants
        /// <summary>
        /// Repository selection scrollview options
        /// </summary>
        private static readonly GUILayoutOption[] scrollOptions = { GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(false), GUILayout.MinHeight(100f), GUILayout.MaxHeight(float.MaxValue), GUILayout.Width(450f) };
        #endregion

        #region Fields
        //Owners
        private readonly ReadOnlyCollection<RepositoryOwner> owners;
        private readonly GitHubAuthenticator authenticator;
        private readonly BuildToolWindow window;
        private Vector2 scroll;
        #endregion

        #region Properties
        /// <summary>
        /// Total amount of Repositories in this selector
        /// </summary>
        public int TotalRepos => this.owners.Sum(o => o.Repositories.Count);

        private RepositoryOwner selectedOwner;
        /// <summary>
        /// The currently selected Repository owner
        /// </summary>
        public RepositoryOwner SelectedOwner
        {
            get => this.selectedOwner;
            private set
            {
                //If the selected owner is not the current owner
                if (this.selectedOwner != value)
                {
                    //Clear the old owner's selection and select the new one
                    this.selectedOwner?.Deselect();
                    this.selectedOwner = value;
                }
            }
        }

        /// <summary>
        /// The currently selected Repository
        /// </summary>
        public Repository SelectedRepository => this.SelectedOwner?.Selected?.Repository;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new RepositorySelector from the given owners
        /// </summary>
        /// <param name="owners">Owners to select from</param>
        /// <param name="authenticator">The GitHubAuthenticator associated to this Control</param>
        /// <param name="window">The window this selector acts on</param>
        public RepositorySelector(IList<RepositoryOwner> owners, GitHubAuthenticator authenticator, BuildToolWindow window)
        {
            //Setup object
            this.owners = new ReadOnlyCollection<RepositoryOwner>(owners);
            this.authenticator = authenticator;
            this.window = window;

            //Get any selected owner if there is one
            this.selectedOwner = this.owners.SingleOrDefault(o => o.Selected != null);
        }
        #endregion

        #region Methods
        /// <summary>
        /// Repository selector UI function
        /// </summary>
        public void OnGUI()
        {
            //Repository selection scrollview
            EditorGUILayout.BeginVertical();
            this.scroll = EditorGUILayout.BeginScrollView(this.scroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUI.skin.scrollView, scrollOptions);
            //Owner panels
            foreach (RepositoryOwner owner in this.owners)
            {
                //If the owner has a currently selected repository, apply it
                if (owner.Select())
                {
                    this.SelectedOwner = owner;
                }
            }
            EditorGUILayout.EndScrollView();
            GUILayout.FlexibleSpace();

            //Select button to apply
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space();
            GUI.enabled = !this.authenticator.FetchingBranches && this.SelectedRepository != null && this.SelectedRepository != this.authenticator.CurrentRepository && this.window.UIEnabled;
            if (GUILayout.Button("Select", GUILayout.Height(30f)))
            {
                //Set repository
                this.Log(this.SelectedRepository?.Name);  //The conditional isn't necessary, but it calms ReSharper down
                this.authenticator.SetBuildRepository();
            }
            GUI.enabled = this.window.UIEnabled;
            EditorGUILayout.Space();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            GUILayout.Space(20f);
        }
        #endregion
    }
}
