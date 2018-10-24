using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

using Deduplicator.Common;
using Windows.Storage.AccessCache;
using System.ComponentModel;



// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Deduplicator
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class WhereToSearch : Page
    {

        MainPage _mainPage = MainPage.Current;
        public DataModel _dataModel;

        public WhereToSearch()
        {
           InitializeComponent();
           _dataModel = MainPage.Current.DataModel;
           listvew_Folders.ItemsSource = _dataModel.FoldersCollection;

            _dataModel.PropertyChanged += _dataModel_PropertyChanged;
            _mainPage.ButtonTapped += OnButtonTapped;
        }

        private void OnButtonTapped(object sender, TappedRoutedEventArgs e)
        {
            if (((Button)sender).Name == "button_DelFolder")
            {
                List<Folder> fl = new List<Folder>();

                foreach (Folder f in listvew_Folders.SelectedItems)
                    fl.Add(f);
                foreach (Folder f in fl)
                {
                    StorageApplicationPermissions.FutureAccessList.Remove(f.AccessToken);
                    _dataModel.FoldersCollection.Remove(f);
                }
            }
        }

        private void _dataModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "FoldersCount":
                    textblock_NoFoldersSelected.Visibility =  (_dataModel.FoldersCount==0) ? Visibility.Visible : Visibility.Collapsed;
                    break;
            }
        }

        private void toggleswitch_SetPrimary_Toggled(object sender, RoutedEventArgs e)
        {
            Folder cf = (sender as ToggleSwitch).DataContext as Folder;
            
            if (cf == null)
                return;
            
            String CurrentFolderName = cf.FullName;
            // Если выбранный фолдер установлен как Primary  то сбросим флажок на остальных фолдерах 
            if (cf.IsPrimary)
            {
                _mainPage.DataModel.PrimaryFolder = cf;
                foreach (Folder f in _mainPage.DataModel.FoldersCollection)
                {
                    if (f.FullName != cf.FullName)
                        f.IsPrimary = false;
                }
            }
            else
            {
                _mainPage.DataModel.PrimaryFolder = null;
            }
        }

        private void listvew_Folders_Tapped(object sender, TappedRoutedEventArgs e)
        {
           _mainPage.button_DelFolderSetIsEnabled( (listvew_Folders.SelectedItems.Count > 0) ? true : false);
        }
    }
}
