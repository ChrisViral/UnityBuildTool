using Octokit;
using UnityEditor;
using UnityEditor.AnimatedValues;

namespace UnityBuildTool.UI
{
    /// <summary>
    /// Repository info UI Toggle
    /// </summary>
    public class RepositoryInfo
    {
        #region Fields
        //GUI Fields
        private readonly AnimBool fadeBool = new AnimBool(false);
        #endregion

        #region Properties
        /// <summary>
        /// The repository associated to this Toggle
        /// </summary>
        public Repository Repository { get; }

        /// <summary>
        /// If this RepositoryInfo is toggled
        /// </summary>
        public bool Toggled
        {
            get => this.fadeBool.target;
            set => this.fadeBool.target = value;
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new RepositoryInfo from the given Repository
        /// </summary>
        /// <param name="repository">Repository to create the info from</param>
        /// <param name="window">The BuildToolWindow this repository toggle works on</param>
        public RepositoryInfo(Repository repository, BuildToolWindow window)
        {
            //Store the repository
            this.Repository = repository;
            //Attach the window to the repaint
            this.fadeBool.valueChanged.AddListener(window.Repaint);

            //If already selected, make sure it's toggled
            if (window.Settings.BuildRepository == repository.FullName) { this.fadeBool.value = true; }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Creates a repository toggle
        /// </summary>
        /// <returns>The toggled state of the control</returns>
        public bool Toggle()
        {
            //Display toggle as repo name
            bool requested = EditorGUILayout.ToggleLeft(this.Repository.Name, this.fadeBool.target, EditorStyles.boldLabel);
            if (!this.Toggled)
            {
                this.Toggled = requested;
            }

            //Fade in/out
            using (EditorGUILayout.FadeGroupScope fade = new EditorGUILayout.FadeGroupScope(this.fadeBool.faded))
            {
                if (fade.visible)
                {
                    //Display repository info when selected
                    EditorGUI.indentLevel++;
                    EditorGUIUtility.labelWidth = 120f;
                    EditorGUILayout.LabelField("Description:", this.Repository.Description, EditorStyles.wordWrappedLabel);
                    EditorGUILayout.LabelField("Creation date:", this.Repository.CreatedAt.Date.ToShortDateString(), EditorStyles.wordWrappedLabel);
                    EditorGUILayout.LabelField("License:", this.Repository.License?.Name ?? "None", EditorStyles.wordWrappedLabel);
                    EditorGUIUtility.labelWidth = 0f;
                    EditorGUI.indentLevel--;
                    EditorGUILayout.Space();
                }
            }

            //Return the state of the toggle
            return this.fadeBool.target;
        }
        #endregion
    }
}