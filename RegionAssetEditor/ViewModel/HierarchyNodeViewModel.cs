using System;
using System.Collections.ObjectModel;

namespace RegionAssetEditor
{
    class HierarchyNodeViewModel
    {
        #region Private Fields



        #endregion  

        #region Public Properties

        /// <summary>
        /// Collection of all the children on this node
        /// </summary>
        public ObservableCollection<HierarchyNodeViewModel> AllChildren { get; private set; }

        /// <summary>
        /// Collection of the children that should be displayed
        /// </summary>
        public ObservableCollection<HierarchyNodeViewModel> DisplayChildren { get; set; }

        /// <summary>
        /// True if the node is expanded. False if it is not.
        /// </summary>
        public bool IsExpanded { get; set; }

        /// <summary>
        /// If the node has children or not. By Default this will be true, and will only be checked when all children is set.
        /// </summary>
        public bool HasChildren { get; set; }

        #endregion 
    }
}
