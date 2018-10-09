using Deduplicator.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Deduplicator
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Options : Page
    {
        private DataModel _dataModel = MainPage.Current.DataModel;
        private MainPage _mainPage = MainPage.Current;

        public Options()
        {
            this.InitializeComponent();
            
            grid_Options.DataContext = _dataModel.Settings;
            _mainPage.ButtonTapped += OnButtonTapped;
        }


        private void OnButtonTapped(object sender, TappedRoutedEventArgs e)
        {
            if (((Button)sender).Name == "button_SaveSettings")
            {
                _dataModel.Settings.Save();
            }
        }
    }
}
