using System;
using System.Collections.ObjectModel;

namespace RegionAssetEditor
{
    class HierarchyNodeViewModel : BaseViewModel
    {
        #region Private Fields

        private bool _isExpanded;
        private bool _isChecked;

        private ObservableCollection<HierarchyNodeViewModel> _displayChildren;

        #endregion  

        #region Public Properties

        /// <summary>
        /// Parent to this node
        /// </summary>
        public HierarchyNodeViewModel Parent { get; set; }

        /// <summary>
        /// Collection of all the children on this node
        /// </summary>
        public ObservableCollection<HierarchyNodeViewModel> AllChildren { get; set; }

        /// <summary>
        /// Collection of the children that should be displayed
        /// </summary>
        public ObservableCollection<HierarchyNodeViewModel> DisplayChildren {
            get {
                if (AllChildren != null)
                    return AllChildren;

                return _displayChildren;
            }
            set {
                _displayChildren = value;
            }
        }

        /// <summary>
        /// True if the node is expanded. False if it is not.
        /// </summary>
        public bool IsExpanded {
            get { return _isExpanded; }
            set {
                if (_isExpanded == value)
                    return;

                _isExpanded = value;
                OnExpandChanged(this);
            }
        }

        /// <summary>
        /// If the node has children or not. By Default this will be true, and will only be checked when all children is set.
        /// </summary>
        public bool HasChildren { get; set; }

        /// <summary>
        /// If the node is currently checked or not
        /// </summary>
        public bool IsChecked {
            get => _isChecked;
            set {
                if (_isChecked == value)
                    return;
                _isChecked = value;

                // Invert the dirty flag. If this was dirty, the value is changing back to its default so it isn't dirty anymore
                IsDirty = !IsDirty;

                // If the child is set to true, the parent has to be as well
                if (_isChecked && Parent != null && !Parent.IsChecked)
                    Parent.IsChecked = true;

                // For now we only change the children if this node is being set to false. 
                // I will decide what I want to do with it being set to true in the future.
                if (AllChildren != null && !_isChecked) {
                    foreach (var child in AllChildren) {
                        child.IsChecked = false;
                    }
                }
            }
        }

        /// <summary>
        /// True if this nodes checked state has been altered. False if it hasn't
        /// </summary>
        public bool IsDirty { get; set; }

        /// <summary>
        /// Text to display for this node
        /// </summary>
        public string NodeText { get; set; }

        /// <summary>
        /// ID used to refer to this node
        /// </summary>
        public int NodeID { get; set; }

        #endregion

        #region Events

        /// <summary>
        /// Event that fires after the node has been expanded or collapsed
        /// </summary>
        public event Action<HierarchyNodeViewModel> OnExpandChanged;

        #endregion
    }
}
