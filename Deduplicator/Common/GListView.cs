using System.ComponentModel;
using Windows.UI.Xaml.Controls;

namespace Deduplicator.Common
{
    public class GListView:ListView, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private double _internalWidth = 200;
        public double InternalWidth
        {
            get { return _internalWidth; }
            set
            {
                if (_internalWidth!=value)
                {
                    _internalWidth = value;
                    NotifyPropertyChanged("InternalWidth");
                }
            }
        }
    }
}
