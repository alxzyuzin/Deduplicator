using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Storage.AccessCache;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml.Input;
using Deduplicator;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Deduplicator
{
  
    public sealed partial class MainPage : Page
    {
        [Flags]
        private enum CmdButtons
        {
            SearchLocation = 1,
            SearchOptions = 2,
            SearchResults = 4,
            Settings = 8,
         }

        private ResourceLoader _resldr = new ResourceLoader();

         public MainPage()
        {
            this.InitializeComponent();
            //ApplicationViews.View g = ApplicationViews.View.WhereToSearch;
            SwitchVewTo(ApplicationViews.View.WhereToSearch);
            this.Unloaded += MainPage_Unloaded;
        }

        private void MainPage_Unloaded(object sender, RoutedEventArgs e)
        {
            StorageApplicationPermissions.FutureAccessList.Clear();
        }

        private void button_SearchLocation_Tapped(object sender, TappedRoutedEventArgs e)
        {
            SwitchVewTo(ApplicationViews.View.WhereToSearch);
        }

        private void button_SearchOptions_Tapped(object sender, TappedRoutedEventArgs e)
        {
            SwitchVewTo(ApplicationViews.View.SearchOptions);
        }

        private void button_SearchResults_Tapped(object sender, TappedRoutedEventArgs e)
        {
            SwitchVewTo(ApplicationViews.View.SearchResults);
        }

        private void button_Settings_Tapped(object sender, TappedRoutedEventArgs e)
        {
            SwitchVewTo(ApplicationViews.View.Settings);
        }

        private void SwitchVewTo(ApplicationViews.View selectedView)
        {
            appViews.SwitchTo(selectedView);
            button_SearchLocation.IsEnabled = selectedView.HasFlag(ApplicationViews.View.WhereToSearch) ? false : true;
            button_SearchOptions.IsEnabled =  selectedView.HasFlag(ApplicationViews.View.SearchOptions) ? false : true;
            button_SearchResults.IsEnabled =  selectedView.HasFlag(ApplicationViews.View.SearchResults) ? false : true;
            button_Settings.IsEnabled =       selectedView.HasFlag(ApplicationViews.View.Settings) ? false : true;
        }
    }
}


