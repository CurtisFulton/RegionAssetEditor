using MEXModel;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace RegionAssetEditor
{
    internal class RegionListViewModel
    {
        public ObservableCollection<Region> AllRegions { get; set; }

        private int _selectedIndex;
        public int SelectedIndex {
            get { return _selectedIndex; }
            set {
                if (_selectedIndex == value)
                    return;

                _selectedIndex = value;
                OnSelectedIndexChanged(value);
            }
        }

        public event Action<int> OnSelectedIndexChanged = (val) => { };

        private DataToken DataToken { get; set; }

        public RegionListViewModel(DataToken dataToken)
        {
            DataToken = dataToken;
            AllRegions = new ObservableCollection<Region>(DataToken.Regions.Where(region => region.RegionID != 1).OrderBy(region => region.RegionName).ToList());

            SelectedIndex = 0;
            OnSelectedIndexChanged(0);
        }
    }
}
