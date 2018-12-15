using System.ComponentModel;
using Windows.UI.Xaml.Controls;

namespace Deduplicator.Common
{
    public class GListView:ListView, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
 
        private double _internalWidth = 200;
        public double InternalWidth
        {
            get { return _internalWidth; }
            set
            {
                if (_internalWidth!=value)
                {
                    _internalWidth = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InternalWidth)));
                 }
            }
        }
    }
}
