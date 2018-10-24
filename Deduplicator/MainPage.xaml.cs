using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Storage.AccessCache;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml.Input;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Deduplicator
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 

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

        //ApplicationTabs appTabs = new ApplicationTabs();

        public MainPage()
        {
            this.InitializeComponent();
           // WorkArea.Child = appTabs;
            appTabs.SwitchTo(ApplicationTabs.Tabs.WhereToSearch);
            DisableButton(ApplicationTabs.Tabs.WhereToSearch);
            this.Unloaded += MainPage_Unloaded;
        }

        private void MainPage_Unloaded(object sender, RoutedEventArgs e)
        {
            StorageApplicationPermissions.FutureAccessList.Clear();
        }

        private void button_SearchLocation_Tapped(object sender, TappedRoutedEventArgs e)
        {
            appTabs.SwitchTo(ApplicationTabs.Tabs.WhereToSearch);
            DisableButton(ApplicationTabs.Tabs.WhereToSearch);
        }

        private void button_SearchOptions_Tapped(object sender, TappedRoutedEventArgs e)
        {
            appTabs.SwitchTo(ApplicationTabs.Tabs.SearchOptions);
            DisableButton(ApplicationTabs.Tabs.SearchOptions);
        }

        private void button_SearchResults_Tapped(object sender, TappedRoutedEventArgs e)
        {
            appTabs.SwitchTo(ApplicationTabs.Tabs.SearchResults);
            DisableButton(ApplicationTabs.Tabs.SearchResults);
        }

        private void button_Settings_Tapped(object sender, TappedRoutedEventArgs e)
        {
            appTabs.SwitchTo(ApplicationTabs.Tabs.Settings);
            DisableButton(ApplicationTabs.Tabs.Settings);
        }


        private void DisableButton(ApplicationTabs.Tabs selectedTab)
        {
            button_SearchLocation.IsEnabled = selectedTab == ApplicationTabs.Tabs.WhereToSearch ? false : true;
            button_SearchOptions.IsEnabled  = selectedTab == ApplicationTabs.Tabs.SearchOptions ? false : true;
            button_SearchResults.IsEnabled  = selectedTab == ApplicationTabs.Tabs.SearchResults ? false : true;
            button_Settings.IsEnabled       = selectedTab == ApplicationTabs.Tabs.Settings ? false : true;
        }
      }
}


