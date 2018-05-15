using MEXModel;
using System;

namespace RegionAssetEditor
{
    internal class MainPageViewModel : BaseViewModel
    {
        public HierarchyViewModel AssetHierarchy { get; set; }
        public RegionListViewModel RegionList { get; set; }

        private DataToken DataToken { get; set; }

        private ConnectionDetails Terminals { get; set; } = new ConnectionDetails() { URL = "http://192.168.0.82/MEXData_Terminals", Username = "admin", Password = "admin" };
        private ConnectionDetails Build73 { get; set; } = new ConnectionDetails() { URL = "http://192.168.0.82/MEXData_build73", Username = "admin", Password = "admin" };
        private ConnectionDetails Trial { get; set; } = new ConnectionDetails() { URL = "http://trial.mex.com.au/MEX", Username = "admin", Password = "admin" };

        public MainPageViewModel()
        {
            // Set the connection details to use
            ConnectionDetails connection = Trial;
            DataToken = new DataToken(connection.URL, connection.Username, connection.Password);

            DataToken.SaveChangesDefaultOptions = System.Data.Services.Client.SaveChangesOptions.Batch;
            AssetHierarchy = new HierarchyViewModel(DataToken);
            RegionList = new RegionListViewModel(DataToken);

            if (RegionList.AllRegions.Count > 0)
                AssetHierarchy.CurrentRegionID = RegionList.AllRegions[0].RegionID;

            RegionList.OnSelectedIndexChanged += (newID) => {
                AssetHierarchy.CurrentRegionID = RegionList.AllRegions[newID].RegionID;
            };
        }
    }

    internal struct ConnectionDetails
    {
        public string URL { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
