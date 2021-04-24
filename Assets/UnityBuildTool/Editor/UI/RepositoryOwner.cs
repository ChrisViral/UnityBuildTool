using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Octokit;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;

namespace UnityBuildTool.UI
{
    /// <summary>
    /// Owner/repositories owner classification
    /// </summary>
    public class RepositoryOwner : IComparable<RepositoryOwner>
    {
        #region Constants
        private static readonly GUILayoutOption[] scopeOptions = { GUILayout.Width(425f) };
        #endregion

        #region Fields
        //GUI fields
        private readonly GUIContent foldoutTitle;
        private readonly AnimBool fadeBool = new AnimBool(false);
        #endregion

        #region Properties
        /// <summary>
        /// Repository owner
        /// </summary>
        public User Owner { get; }

        /// <summary>
        /// Owned repositories by the User
        /// </summary>
        public ReadOnlyCollection<RepositoryInfo> Repositories { get; }

        private RepositoryInfo selected;
        /// <summary>
        /// The currently selected Repository
        /// </summary>
        public RepositoryInfo Selected
        {
            get => this.selected;
            private set
            {
                //If the selected value is different
                if (this.selected != value)
                {
                    //If there already is a selected repo
                    if (this.selected != null)
                    {
                        //Unselect it
                        this.selected.Toggled = false;
                    }
                    //Then select the new one
                    this.selected = value;
                }
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new RepositoryOwner with the given owner and repository list
        /// </summary>
        /// <param name="owner">Owner of the repositories</param>
        /// <param name="repositories">Repositories owner</param>
        /// <param name="window">The BuildToolWindow this Owner folder operates on</param>
        public RepositoryOwner(User owner, IList<RepositoryInfo> repositories, BuildToolWindow window)
        {
            //Info
            this.Owner = owner;
            this.Repositories = new ReadOnlyCollection<RepositoryInfo>(repositories);
            this.fadeBool.valueChanged.AddListener(window.Repaint);

            //UI content
            this.foldoutTitle = new GUIContent(owner.Login, "Repositories owned by " + owner.Login);

            //Check if any children repository is already selected
            this.selected = this.Repositories.SingleOrDefault(r => r.Toggled);
            //If there is, make sure the foldout is expanded
            if (this.selected != null)
            {
                this.fadeBool.value = true;
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// UI Selection control for the Owner
        /// </summary>
        /// <returns>The selected repository or null if none is</returns>
        public bool Select()
        {
            //Foldout group
            using (VerticalScope.Enter(scopeOptions))
            using (FoldoutHeaderScope.Enter(this.fadeBool, this.foldoutTitle))
            using (FadeGroupScope.Enter(this.fadeBool.faded, out bool visible))
            {
                if (visible)
                {
                    //Check for repository selection
                    bool oneSelected = false;
                    EditorGUI.indentLevel++;
                    //Display the repositories of the owner
                    foreach (RepositoryInfo repo in this.Repositories)
                    {
                        //If the repo is toggled
                        if (repo.Toggle())
                        {
                            //Flag and set selection
                            oneSelected = true;
                            this.Selected = repo;
                        }
                    }

                    EditorGUI.indentLevel--;
                    //If none is selected, make sure the selection is null
                    if (!oneSelected)
                    {
                        this.Selected = null;
                    }
                }
            }

            //Return true if any is selected
            return this.Selected != null;
        }

        /// <summary>
        /// Deselects the current repository
        /// </summary>
        public void Deselect() => this.Selected = null;

        /// <summary>
        /// Compares this instance to the other to determine sort order
        /// </summary>
        /// <param name="other">Other RepositoryOwner to compare to</param>
        /// <returns>An int value indicating order</returns>
        public int CompareTo(RepositoryOwner other) => string.CompareOrdinal(this.Owner.Login, other.Owner.Login);
        #endregion
    }
}