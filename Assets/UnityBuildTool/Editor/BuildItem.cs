using System;
using UnityEngine;

//ReSharper disable UnassignedField.Local

namespace UnityBuildTool
{
    /// <summary>
    /// An item to be copied on build
    /// </summary>
    [Serializable]
    public class BuildItem
    {
        /// <summary>
        /// Enum representing where to copy the item
        /// </summary>
        public enum CopyLocation
        {
            Root,
            WithAppData,
            InAppData
        }

        #region Constants
        /// <summary>Name of the Path property</summary>
        public const string PATH_NAME = nameof(path);

        /// <summary>Name of the Location property</summary>
        public const string LOCATION_NAME = nameof(location);
        #endregion

        #region Properties
        [SerializeField]
        private string path;
        /// <summary>
        /// Path to the item to copy
        /// </summary>
        public string Path => this.path;

        [SerializeField]
        private CopyLocation location;
        /// <summary>
        /// Location to copy the item
        /// </summary>
        public CopyLocation Location => this.location;
        #endregion
    }
}