using Deduplicator.Common;
using System;
using System.Collections.Generic;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Deduplicator
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SearchResults : Page
    {
        ResourceLoader _resldr = new ResourceLoader();

        private DataModel _dataModel;// = MainPage.Current.DataModel;
        private MainPage _mainPage; // = MainPage.Current;
        private bool _doNotRegroup = true;
        public SearchResults()
        {
            this.InitializeComponent();

            var _previousSelectedItem = listbox_ResultGrouping.SelectedItem;
            GroupedFiles.Source = _dataModel.ResultFilesCollection;
            listbox_ResultGrouping.ItemsSource = _dataModel.ResultGrouppingModes;
             
            if (_dataModel.ResultGrouppingModes.Count > 0)
                listbox_ResultGrouping.SelectedItem = _dataModel.CurrentGroupMode;
            listview_Duplicates.InternalWidth = _mainPage.ActualWidth - 80;

            //_mainPage.UpdateDeleteSelectedFilesButton(listview_Duplicates.SelectedItems.Count);

            if (_dataModel.PrimaryFolder != null)
            {
                txtblk_GroupBy.Text = "Duplicates grouped by files in primary folder";
                listbox_ResultGrouping.Visibility = Visibility.Collapsed;
            }
            else
            {
                txtblk_GroupBy.Text = "Group files by";
                listbox_ResultGrouping.Visibility = Visibility.Visible;
            }
//            _dataModel.SearchStarted += OnDataModel_SearchStarted;
//            _dataModel.SearchCompleted += OnDataModel_SearchCompleted;
//            _mainPage.ButtonTapped += OnButtonTapped;
            _mainPage.SizeChanged += _mainPage_SizeChanged;
        }

        private void _mainPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            listview_Duplicates.InternalWidth = _mainPage.ActualWidth-80;
        }

        private void OnDataModel_SearchStarted(object sender, EventArgs e)
        {
            GroupedFiles.Source = null;
        }

        private void OnDataModel_SearchCompleted(object sender, EventArgs e)
        {
            GroupedFiles.Source = _dataModel.ResultFilesCollection;
        }

        private void listview_Duplicates_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (listview_Duplicates.SelectedItems.Contains(e.ClickedItem))
                listview_Duplicates.SelectedItems.Remove(e.ClickedItem);
            else
                listview_Duplicates.SelectedItems.Add(e.ClickedItem);
           // _mainPage.UpdateDeleteSelectedFilesButton(listview_Duplicates.SelectedItems.Count);
        }

        private void checkbox_SelectGroup_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox cbx = sender as CheckBox;
            FilesGroup datacontext = cbx.DataContext as FilesGroup;
            foreach ( File f in datacontext)
            {
                listview_Duplicates.SelectedItems.Add(f);
            }
            //_mainPage.UpdateDeleteSelectedFilesButton(listview_Duplicates.SelectedItems.Count);
        }

        private void checkbox_SelectGroup_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox cbx = sender as CheckBox;
            FilesGroup datacontext = cbx.DataContext as FilesGroup;
            foreach (File f in datacontext)
            {
                listview_Duplicates.SelectedItems.Remove(f);
            }
            //_mainPage.UpdateDeleteSelectedFilesButton(listview_Duplicates.SelectedItems.Count);
        }
       
        private void listbox_ResultGrouping_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_doNotRegroup)
            {
                _doNotRegroup = false;
                return;
            }

            ComboBox cb = sender as ComboBox;
            if (cb.SelectedIndex >= 0)
            {
                _dataModel.CurrentGroupMode = (string)cb.SelectedValue;

                FileAttribs grpatr = DataModel.ConvertGroupingNameToFileAttrib(cb.SelectedValue.ToString());
                _dataModel.RegroupResultsByFileAttribute(grpatr);
                _dataModel.ResultFilesCollection.Invalidate();
//                _dataModel.SearchStatus = string.Format("Regrouping complete. Regrouped {0} duplicates into {1} groups.",
//                                                       _dataModel.TotalDuplicatesCount, 
//                                                       _dataModel.ResultFilesCollection.Count);
                _dataModel.ResultFilesCollection.Invalidate();
            }
        }

        private async void OnButtonTapped(object sender, TappedRoutedEventArgs e)
        {
            if (((Button)sender).Name == "button_DeleteSelectedFiles")
            {
                List<File> deletedFiles = new List<File>();
                foreach (File file in listview_Duplicates.SelectedItems)
                {
                    if (!file.IsProtected)
                    {
                        StorageFile f = await StorageFile.GetFileFromPathAsync(file.FullName);
                        await f.DeleteAsync(StorageDeleteOption.Default);
                        deletedFiles.Add(file);
                    }
                
                }
                foreach (File file in deletedFiles)
                {
                    foreach (var group in _dataModel.ResultFilesCollection)
                    {
                        if (group.Contains(file))
                            group.Remove(file);
                    }
                }
            }
        }
    }
}

