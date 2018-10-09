using Deduplicator.Common;
using System.ComponentModel;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Deduplicator
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SearchOptions : Page
    {
        MainPage mainPage = MainPage.Current;
        public DataModel _dataModel;

        public SearchOptions()
        {
            this.InitializeComponent();

            _dataModel = MainPage.Current.DataModel;

            stackpanel_FileSelectionOptions.DataContext = _dataModel.FileSelectionOptions;
            stackpanel_FileCompareOptions.DataContext = _dataModel.FileCompareOptions;

            listbox_ResultGroupingModes.ItemsSource = _dataModel.ResultGrouppingModes;
            _dataModel.UpdateResultGruppingModesList();
            listbox_ResultGroupingModes.SelectedItem = _dataModel.ResultGrouppingModes[0];


            _dataModel.PropertyChanged += _dataModel_PropertyChanged;
        }

        private void _dataModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "FileCompareOptions":
                    _dataModel.UpdateResultGruppingModesList();
                    listbox_ResultGroupingModes.SelectedItem = (_dataModel.ResultGrouppingModes.Count > 0) ? _dataModel.ResultGrouppingModes[0] : null;
                    break;
            }
        }

        private void listbox_ResultGroupingModes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _dataModel.CurrentGroupMode = ((string)((ComboBox)sender).SelectedValue);
        }

    }
}
