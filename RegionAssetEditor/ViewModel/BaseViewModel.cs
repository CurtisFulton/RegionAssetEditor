using System;
using System.ComponentModel;

namespace RegionAssetEditor
{
    class BaseViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Property Changed Event. This should never need to be manually called
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged = (sender, e) => { } ;

        /// <summary>
        /// Helper function to force update a property
        /// </summary>
        /// <param name="propertyName">Name of the property that has been changed</param>
        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
