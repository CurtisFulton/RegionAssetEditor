using MEXModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace RegionAssetEditor
{
    internal class HierarchyViewModel : BaseViewModel
    {
        private DataToken DataToken { get; set; }

        private List<AssetModel> AllAssets { get; set; }
        private Dictionary<int, HierarchyNodeViewModel> AssetToNode { get; set; } = new Dictionary<int, HierarchyNodeViewModel>();

        private List<RegionAssetModel> AllRegionAssets { get; set; }

        private int _currentRegionID;
        public int CurrentRegionID {
            get => _currentRegionID;
            set {
                if (_currentRegionID == value)
                    return;

                SaveChanges();
                _currentRegionID = value;
                // Load new region data in
                List<HierarchyNodeViewModel> allNodes = AllAssets.Where(a => AssetToNode.ContainsKey(a.AssetID))
                                                 .Select(a => AssetToNode[a.AssetID]).ToList();

                AllRegionAssets = GetRegionData(_currentRegionID);
                allNodes.ForEach(n => LoadRegionDataIntoNode(n));
            }
        }
        
        public ObservableCollection<HierarchyNodeViewModel> Level1Nodes { get; set; }
        
        public HierarchyViewModel(DataToken dataToken)
        {
            DataToken = dataToken;

            SetupHierarchy();
        }

        /// <summary>
        /// Resets everything and creates the hierarchy from scratch
        /// </summary>
        private void SetupHierarchy()
        {
            // Only get some of the information from the asset
            string query = @"
                SELECT AssetID, ParentAssetID, AssetNumber
                FROM Asset
                WHERE IsActive = 1 AND IsAsset = 1
                ORDER BY AssetNumber
            ";
            query = Regex.Replace(query, @"\s+", " ").Trim();
            
            AllAssets = DataToken.DynamicQuery<AssetModel>(query);

            // Get only the level 1 assets from this list
            List<AssetModel> level1Assets = AllAssets.Where(a => a.ParentAssetID == 0 && a.AssetNumber != "Mex Inspections").ToList();
            Level1Nodes = new ObservableCollection<HierarchyNodeViewModel>(level1Assets.Select(a => GetNodeFromAsset(a)));
        }

        /// <summary>
        /// Gets the Region Asset data and returns it as a list
        /// </summary>
        /// <param name="regionID">Region ID to load</param>
        /// <returns>The Region Asset Data as a list</returns>
        private List<RegionAssetModel> GetRegionData(int regionID)
        {
            string query = @"
                SELECT AssetID
                FROM RegionAsset
                WHERE RegionID = " + CurrentRegionID;

            query = Regex.Replace(query, @"\s+", " ").Trim();
            
            return DataToken.DynamicQuery<RegionAssetModel>(query);
        }

        /// <summary>
        /// Event that is fired when the nodes expanded state has changed
        /// </summary>
        /// <param name="node">Node that was expanded</param>
        private void OnNodeExpandChanged(HierarchyNodeViewModel node)
        {
            // We only have to do something if the node is now in the expanded state
            if (node.IsExpanded)
                OnNodeExpanded(node);
        }

        /// <summary>
        /// Event that is fired when the node has expanded
        /// </summary>
        /// <param name="node">Node that was expanded</param>
        private void OnNodeExpanded(HierarchyNodeViewModel node)
        {
            // If the children nodes have already been set we can just return
            if (node.AllChildren != null)
                return;

            // Find all the children assets
            List<AssetModel> assetChildren = AllAssets.Where(a => a.ParentAssetID == node.NodeID).ToList();
            node.AllChildren = new ObservableCollection<HierarchyNodeViewModel>();

            // Get/Create all the children nodes and add them to the expanded nodes 'AllChildren'
            foreach (var child in assetChildren) {
                HierarchyNodeViewModel childNode = GetNodeFromAsset(child);

                childNode.Parent = node;
                node.AllChildren.Add(childNode);
            }
        }
        
        /// <summary>
        /// Saves all changes currently made
        /// </summary>
        public void SaveChanges()
        {
            // If there are no assets loaded, there is no changes to save
            if (AllAssets == null) {
                return;
            }
            
            // Get all nodes currently loaded
            List<HierarchyNodeViewModel> allNodes = AllAssets.Where(a => AssetToNode.ContainsKey(a.AssetID))
                                                             .Select(a => AssetToNode[a.AssetID]).ToList();

            // Get all the dirty nodes and save changes for each node
            List<HierarchyNodeViewModel> dirtyNodes = allNodes.Where(node => node.IsDirty).ToList();

            HashSet<HierarchyNodeViewModel> addedNodes = new HashSet<HierarchyNodeViewModel>();
            HashSet<int> regionAssetsToDelete = new HashSet<int>();

            dirtyNodes.ForEach(n => {
                // Create a RegionAsset object and add to datatoken
                if (n.IsChecked) {
                    // Recursive function, need to make sure all of this node are added
                    void AddNodeRegionAsset(HierarchyNodeViewModel node)
                    {
                        if (addedNodes.Contains(node))
                            return;

                        // Add this to the 
                        RegionAsset newRegionAsset = RegionAsset.CreateRegionAsset(0, CurrentRegionID, node.NodeID, -1, DateTime.Now, -1, DateTime.Now);
                        DataToken.AddToRegionAssets(newRegionAsset);
                        addedNodes.Add(node);

                        bool previousExpandState = node.IsExpanded;
                        node.IsExpanded = true;
                        
                        foreach (var childNode in node.AllChildren) {
                            if (childNode.IsChecked)
                                AddNodeRegionAsset(childNode);
                        }

                        node.IsExpanded = previousExpandState;
                    }

                    // Recursively add all children
                    AddNodeRegionAsset(n);
                } else {
                    // Recursive Function, need to make sure that all the children are removed
                    void RemoveNodeFromRegionAsset(HierarchyNodeViewModel node)
                    {
                        RegionAssetModel regionAsset = AllRegionAssets.FirstOrDefault(ra => ra.AssetID == node.NodeID);

                        if (regionAsset != null && !regionAssetsToDelete.Contains(node.NodeID))
                            regionAssetsToDelete.Add(regionAsset.AssetID);

                        List<AssetModel> childrenAssets = AllAssets.Where(a => a.ParentAssetID == node.NodeID).ToList();
                        foreach (var child in childrenAssets) {
                            HierarchyNodeViewModel childNode = GetNodeFromAsset(child);

                            if ((childNode.IsDirty && !childNode.IsChecked) || (!childNode.IsDirty && childNode.IsChecked))
                                RemoveNodeFromRegionAsset(childNode);
                        }
                    }

                    RemoveNodeFromRegionAsset(n);
                }
            });
            
            if (regionAssetsToDelete.Count > 0) {
                var test = string.Join(",", regionAssetsToDelete);

                string deleteQuery = @"
                DELETE FROM RegionAsset
                WHERE RegionID =" + CurrentRegionID + " AND AssetID IN (" + string.Join(",", regionAssetsToDelete) + ")";
                deleteQuery = Regex.Replace(deleteQuery, @"\s+", " ").Trim();

                // This is Async, and because we don't await it the program will continue. This will increase loading when switching regions
                var response = DataToken.DynamicQueryAsync(deleteQuery);
            }

            // If there were no dirty nodes, we saved no data
            if (dirtyNodes.Count > 0)
                DataToken.SaveChanges();
        }

        /// <summary>
        /// Loads the region data into this node. Does checks on the parent to see if it should override its default value
        /// </summary>
        /// <param name="node">Node to load the data into</param>
        private void LoadRegionDataIntoNode(HierarchyNodeViewModel node)
        {
            // If there is no RegionAsset data, we have nothing to load in
            if (AllRegionAssets == null)
                return;

            bool hasRegionAsset = AllRegionAssets.FirstOrDefault(ra => ra.AssetID == node.NodeID) != null;

            // If the parent is dirty, we want to update this node to be the same value. 
            if (node.Parent != null && node.Parent.IsDirty) {
                node.IsChecked = node.Parent.IsChecked;
                
                // If this node is a different value from what it was, it is dirty
                node.IsDirty = node.IsChecked ? !hasRegionAsset : hasRegionAsset;
            } else {
                // Check the region data to see if this one should be checked or not
                node.IsChecked = hasRegionAsset;
                node.IsDirty = false;
            }
        }

        /// <summary>
        /// Gets the node corrisponding to the node. Creates the node if it doesn't already exist.
        /// </summary>
        /// <param name="asset">Asset to find the node of</param>
        /// <returns>The Node that corrisponds to the asset</returns>
        private HierarchyNodeViewModel GetNodeFromAsset(AssetModel asset)
        {
            // If the node is not in the dictionary, create a new node and add it
            if (!AssetToNode.ContainsKey(asset.AssetID)) {
                HierarchyNodeViewModel newNode = new HierarchyNodeViewModel();

                // Check to see if there are any children that could be loaded
                bool hasChildren = AllAssets.FirstOrDefault(a => a.ParentAssetID == asset.AssetID) != null;
                // If there are children, we need to add a dummy collection with 1 empty value so we get the expand arrow
                if (hasChildren)
                    newNode.DisplayChildren = new ObservableCollection<HierarchyNodeViewModel>() { null };

                newNode.NodeText = asset.AssetNumber;
                newNode.NodeID = asset.AssetID;
                
                // If this node has a parent, get it and assign it
                if (asset.ParentAssetID != 0) {
                    AssetModel parentAsset = AllAssets.FirstOrDefault(a => a.AssetID == asset.ParentAssetID);
                    HierarchyNodeViewModel parentNode = GetNodeFromAsset(parentAsset);

                    newNode.Parent = parentNode;
                }

                // Load this nodes region data
                LoadRegionDataIntoNode(newNode);

                // Add the event listener
                newNode.OnExpandChanged += OnNodeExpandChanged;

                // Add it to the dictionary so we can get to this node from the asset
                AssetToNode.Add(asset.AssetID, newNode);
            }

            return AssetToNode[asset.AssetID];
        }
        private HierarchyNodeViewModel GetNodeFromAsset(int assetID) => GetNodeFromAsset(AllAssets.FirstOrDefault(a => a.AssetID == assetID));
    }
}