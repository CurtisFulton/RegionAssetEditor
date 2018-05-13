using MEXModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace RegionAssetEditor
{
    internal class HierarchyViewModel : BaseViewModel
    {
        private DataToken DataToken { get; set; }

        private List<AssetModel> AllAssets { get; set; }
        private Dictionary<int, HierarchyNodeViewModel> AssetToNode { get; set; } = new Dictionary<int, HierarchyNodeViewModel>();

        private List<RegionAsset> AllRegionAssets { get; set; }

        private int _currentRegionID;
        private int CurrentRegionID {
            get => _currentRegionID;
            set {
                if (_currentRegionID == value)
                    return;

                _currentRegionID = value;
                SaveChanges();
            }
        }
        
        public ObservableCollection<HierarchyNodeViewModel> Level1Nodes { get; set; }
        
        public HierarchyViewModel()
        {
            DataToken = new DataToken(new Uri("http://trial.mex.com.au/MEXTrial/odata.svc"));

            SetupHierarchy();
            CurrentRegionID = 2;
            CurrentRegionID = 3;
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
            Level1Nodes = new ObservableCollection<HierarchyNodeViewModel>();

            // Get only the level 1 assets from this list
            List<AssetModel> level1Assets = AllAssets.Where(a => a.ParentAssetID == 0 && a.AssetNumber != "MEX Inspections").ToList();

            foreach (var asset in level1Assets) {
                Level1Nodes.Add(GetNodeFromAsset(asset));
            }
        }

        /// <summary>
        /// Gets the Region Asset data and returns it as a list
        /// </summary>
        /// <param name="regionID">Region ID to load</param>
        /// <returns>The Region Asset Data as a list</returns>
        private List<RegionAsset> GetRegionData(int regionID)
        {
            // Might not be needed, but in case I want to add some logic to this.
            return DataToken.RegionAssets.Where(ra => ra.RegionID == regionID).ToList();
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
        
        public void SaveChanges()
        {
            if (AllAssets == null) {
                return;
            }

            // Get all nodes currently loaded
            List<HierarchyNodeViewModel> allNodes = AllAssets.Where(a => AssetToNode.ContainsKey(a.AssetID))
                                                             .Select(a => AssetToNode[a.AssetID]).ToList();

            // Get all the dirty nodes and save changes for each node
            List<HierarchyNodeViewModel> dirtyNodes = allNodes.Where(node => node.IsDirty).ToList();
            dirtyNodes.ForEach(n => SaveNodeChanges(n));

            // Load new region data in
            AllRegionAssets = GetRegionData(_currentRegionID);
            allNodes.ForEach(n => LoadRegionDataIntoNode(n));

            DataToken.SaveChanges();
        }

        private void SaveNodeChanges(HierarchyNodeViewModel node)
        {
            if (node.IsChecked) {
                // Create a RegionAsset object and add to datatoken
                RegionAsset newRegionAsset = RegionAsset.CreateRegionAsset(0, CurrentRegionID, node.NodeID, -1, DateTime.Now, -1, DateTime.Now);
                DataToken.AddToRegionAssets(newRegionAsset);
            } else {
                // Remove the existing regionAsset 
                RegionAsset regionAsset = AllRegionAssets.FirstOrDefault(ra => ra.AssetID == node.NodeID);
                DataToken.DeleteObject(regionAsset);
            }
        }

        private void LoadRegionDataIntoNode(HierarchyNodeViewModel node)
        {
            if (AllRegionAssets == null)
                return;
            
            // Check to see if the parent is disabled. If it is, we need to make sure this node is disabled too
            if (node.Parent != null && !node.Parent.IsChecked) {
                node.IsChecked = false;
                node.IsDirty = true;
            } else {
                // Check the region data to see if this one should be checked or not
                node.IsChecked = AllRegionAssets.FirstOrDefault(ra => ra.AssetID == node.NodeID) != null;
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

                bool hasChildren = AllAssets.FirstOrDefault(a => a.ParentAssetID == asset.AssetID) != null;
                if (hasChildren)
                    newNode.DisplayChildren = new ObservableCollection<HierarchyNodeViewModel>() { null };

                newNode.NodeText = asset.AssetNumber;
                newNode.NodeID = asset.AssetID;
                
                if (asset.ParentAssetID != 0) {
                    AssetModel parentAsset = AllAssets.FirstOrDefault(a => a.AssetID == asset.ParentAssetID);
                    HierarchyNodeViewModel parentNode = GetNodeFromAsset(parentAsset);

                    newNode.Parent = parentNode;
                }

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