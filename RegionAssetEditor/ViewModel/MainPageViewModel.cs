using MEXModel;
using System;

namespace RegionAssetEditor
{
    internal class MainPageViewModel : BaseViewModel
    {
        public HierarchyViewModel AssetHierarchy { get; set; }
        public RegionListViewModel RegionList { get; set; }

        private DataToken DataToken { get; set; }

        public MainPageViewModel()
        {
            //DataToken = new DataToken(new Uri("http://192.168.0.82/MEXData_UMS/odata.svc"), "admin", "PTV@$$Man1");
            //DataToken = new DataToken(new Uri("http://192.168.0.82/MEXData_Terminals/odata.svc"), "admin", "admin");
            DataToken = new DataToken("http://192.168.0.82/MEXData_build73", "admin", "admin");

            DataToken.SaveChangesDefaultOptions = System.Data.Services.Client.SaveChangesOptions.Batch;
            AssetHierarchy = new HierarchyViewModel(DataToken);
            RegionList = new RegionListViewModel(DataToken);

            AssetHierarchy.CurrentRegionID = RegionList.AllRegions[0].RegionID;

            RegionList.OnSelectedIndexChanged += (newID) => {
                AssetHierarchy.CurrentRegionID = RegionList.AllRegions[newID].RegionID;
            };
        }
    }
}
