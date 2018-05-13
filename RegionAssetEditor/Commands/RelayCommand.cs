using System;
using System.Windows.Input;

namespace RegionAssetEditor
{
    internal class RelayCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;
        private event Action RelayEvent = () => { };

        public RelayCommand(Action relayEvent)
        {
            RelayEvent = relayEvent;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            RelayEvent();
        }
    }
}