using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

using Deduplicator.Common;
using System.ComponentModel;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.ApplicationModel.Resources;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Deduplicator
{
    public  sealed partial class ApplicationTabs : UserControl, INotifyPropertyChanged
    {

        [Flags]
        private enum CmdButtons
        {
            DelSelectedFiles =  1,
            StartSearch      =  2,
            CancelSearch     =  4,
            SaveSettings     =  8,
            AddFolder        = 16,
            DelFolder        = 32
        }

        [Flags]
        public enum Tabs { WhereToSearch = 1, SearchOptions = 2, SearchResults = 4, Settings = 8}

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        Tabs _activeTab;
        private ResourceLoader _resldr = new ResourceLoader();

        CmdButtons _cmdButtonsVisualState;
        private CmdButtons CmdButtonsVisualState
        {
            set
            {
                if (_cmdButtonsVisualState != value)
                {
                    _cmdButtonsVisualState = 0;
                    _cmdButtonsVisualState |= value;

                    NotifyPropertyChanged("BtnDelSelectedFilesVisibility");
                    NotifyPropertyChanged("BtnStartSearchVisibility");
                    NotifyPropertyChanged("BtnCancelSearchVisibility");
                    NotifyPropertyChanged("BtnSaveSettingsVisibility");
                    NotifyPropertyChanged("BtnAddFolderVisibility");
                    NotifyPropertyChanged("BtnDelFolderVisibility");
                }
            }
        }
        
        public Visibility BtnDelSelectedFilesVisibility
        {
            get { return _cmdButtonsVisualState.HasFlag(CmdButtons.DelSelectedFiles) ? Visibility.Visible : Visibility.Collapsed; }
        }

        public Visibility BtnStartSearchVisibility
        {
            get { return _cmdButtonsVisualState.HasFlag(CmdButtons.StartSearch) ? Visibility.Visible : Visibility.Collapsed; }
        }
        public Visibility BtnCancelSearchVisibility
        {
            get { return _cmdButtonsVisualState.HasFlag(CmdButtons.CancelSearch) ? Visibility.Visible : Visibility.Collapsed; }
        }
        public Visibility BtnSaveSettingsVisibility
        {
            get { return _cmdButtonsVisualState.HasFlag(CmdButtons.SaveSettings) ? Visibility.Visible : Visibility.Collapsed; }
        }
        public Visibility BtnAddFolderVisibility
        {
            get { return _cmdButtonsVisualState.HasFlag(CmdButtons.AddFolder) ? Visibility.Visible : Visibility.Collapsed; }
        }
        public Visibility BtnDelFolderVisibility
        {
            get { return _cmdButtonsVisualState.HasFlag(CmdButtons.DelFolder) ? Visibility.Visible : Visibility.Collapsed; }
        }


        public bool _btnAddFolderEnabled = true;
        public bool BtnAddFolderEnabled
        {
            get { return _btnAddFolderEnabled; }
            set
            {
                if (_btnAddFolderEnabled != value)
                {
                    _btnAddFolderEnabled = value;
                    NotifyPropertyChanged("BtnAddFolderEnabled");
                }
            }
        }

        public bool _btnDelFolderEnabled = false;
        public bool BtnDelFolderEnabled
        {
            get { return _btnDelFolderEnabled;}
            set
            {
                if (_btnDelFolderEnabled != value)
                {
                    _btnDelFolderEnabled = value;
                    NotifyPropertyChanged("BtnDelFolderEnabled");
                }
            }
        }


        private bool _btnStartSearchEnabled = false;
        public bool BtnStartSearchEnabled
        {
            get
            {
                return _btnStartSearchEnabled;
            }
            set
            {
                if (_btnStartSearchEnabled != value)
                {
                    _btnStartSearchEnabled = value;
                    NotifyPropertyChanged("BtnStartSearchEnabled");
                }
            }
        }

        private bool _btnStopSearchEnabled = false;
        public bool BtnStopSearchEnabled
        {
            get
            {
                return _btnStopSearchEnabled;

            }
            set
            {
                if (_btnStopSearchEnabled != value)
                {
                    _btnStopSearchEnabled = value;
                    NotifyPropertyChanged("BtnStopSearchEnabled");
                }
            }
        }

        private string _tabHeader = string.Empty;
        public string TabHeader
        {
            get { return _tabHeader; }
            set { _tabHeader = value; NotifyPropertyChanged("TabHeader"); }
        }

        private string _emptyContentMessage;
        public string EmptyContentMessage
        {
            get { return _emptyContentMessage; }
            set { _emptyContentMessage = value; NotifyPropertyChanged("EmptyContentMessage"); }
        }

        private Visibility _emptyContentMessageVisibility;
        public Visibility EmptyContentMessageVisibility
        {
            get
            {
                return _emptyContentMessageVisibility;
            }
            set
            {
                if (_emptyContentMessageVisibility != value)
                {
                    _emptyContentMessageVisibility = value;
                    NotifyPropertyChanged("EmptyContentMessageVisibility");
                }
            }
        }

        private string _applicationStatus;
        public string ApplicationStatus
        {
            get { return _applicationStatus; }
            set { _applicationStatus = value; NotifyPropertyChanged("ApplicationStatus"); }
        }

        private DataModel _dataModel = new DataModel(null);
        //private Page _mainPage;
        
        public ApplicationTabs()
        {
            this.InitializeComponent();
            this.DataContext = this;

            // Tab "Where to search" data binding
            listview_Folders.ItemsSource = _dataModel.FoldersCollection;

            // Tab "Search options" data binding
            stackpanel_FileSelectionOptions.DataContext = _dataModel.FileSelectionOptions;
            stackpanel_FileCompareOptions.DataContext = _dataModel.FileCompareOptions;

            listbox_ResultGroupingModes.ItemsSource = _dataModel.ResultGrouppingModes;
            _dataModel.UpdateResultGruppingModesList();
            listbox_ResultGroupingModes.SelectedItem = _dataModel.ResultGrouppingModes[0];

            // Tab "Search results" data binding
            GroupedFiles.Source = _dataModel.ResultFilesCollection;
            listbox_ResultGrouping.ItemsSource = _dataModel.ResultGrouppingModes;

            // Tab "Settings" data binding
            brd_SettingsTab.DataContext = _dataModel.Settings;

            // Подпишемся на события модели данных
             _dataModel.SearchStatusChanged += OnSearchStatusChanged;

        }

        private void OnSearchStatusChanged(object sender, DataModel.SearchStatus status)
        {
            
            ApplicationStatus = _dataModel.SearchStatusInfo;

            if (status == DataModel.SearchStatus.Completed)
            {
                BtnAddFolderEnabled = true;
                BtnDelFolderEnabled = (listview_Folders.SelectedItems.Count > 0) ? true : false;
                BtnStartSearchEnabled = (listview_Folders.Items.Count > 0) ? true : false;
                BtnStopSearchEnabled = false;
                _dataModel.ResultFilesCollection.Invalidate();
                //listview_Duplicates.ItemsSource = _dataModel.ResultFilesCollection;
                //GroupedFiles.Source = _dataModel.ResultFilesCollection;
            }
            else
            {
                BtnAddFolderEnabled = false;
                BtnDelFolderEnabled = false;
                BtnStartSearchEnabled = false;
                BtnStopSearchEnabled = true;
            }
           
            
        }

        //private void OnSearchCompleted(object sender, EventArgs e)
        //{
        //    ApplicationStatus = "Search completed";
        //}

        //private void OnSearchStarted(object sender, EventArgs e)
        //{
        //    ApplicationStatus = "Search started";
        //}

        /*
public ApplicationTabs(Page page)
{
   this.InitializeComponent();

   PageHeader.Text = "List of folders where to search duplicates";

   EmptyContentMessage.Text = "No folders selected for searching duplicated files.\n Add folders where search duplicates.";

}
*/


        public void SwitchTo(Tabs tab)
        {
            _activeTab = tab;

            WhereToSearchtab.Visibility = Visibility.Collapsed;
            SearchOptionsTab.Visibility = Visibility.Collapsed;
            SearchResultsTab.Visibility = Visibility.Collapsed;
            brd_SettingsTab.Visibility = Visibility.Collapsed;
            stackpanel_GroupingSelector.Visibility = Visibility.Collapsed;

            switch (tab)
            {
                case Tabs.WhereToSearch:
                    WhereToSearchtab.Visibility = Visibility.Visible;
                    TabHeader = "List of folders where to search duplicates.";
                    CmdButtonsVisualState = CmdButtons.AddFolder|CmdButtons.DelFolder|CmdButtons.StartSearch|CmdButtons.CancelSearch;
                    EmptyContentMessageVisibility = (listview_Folders.Items.Count > 0) ? Visibility.Collapsed : Visibility.Visible;
                    EmptyContentMessage = "No folders selected for searching duplicated files.\n Add folders where search duplicates.";
                    break;
                case Tabs.SearchOptions:
                    SearchOptionsTab.Visibility = Visibility.Visible;
                    TabHeader = "Search options.";
                    CmdButtonsVisualState = CmdButtons.StartSearch | CmdButtons.CancelSearch;
                    EmptyContentMessage = string.Empty;
                    break;
                case Tabs.SearchResults:
                    SearchResultsTab.Visibility = Visibility.Visible;
                    TabHeader = "Search results.";
                    stackpanel_GroupingSelector.Visibility = Visibility.Visible;
                    CmdButtonsVisualState = CmdButtons.DelSelectedFiles | CmdButtons.StartSearch | CmdButtons.CancelSearch;
                    EmptyContentMessage = "No duplicates found.";
                    break;
                case Tabs.Settings:
                    brd_SettingsTab.Visibility = Visibility.Visible;
                    TabHeader = "Settings.";
                    CmdButtonsVisualState = CmdButtons.SaveSettings;
                    EmptyContentMessage = string.Empty;
                    break;
            }
        }


        private async void button_AddFolder_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FolderPicker folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
            folderPicker.FileTypeFilter.Add("*");
            StorageFolder newfolder = await folderPicker.PickSingleFolderAsync();

            MsgBox.SetButtons(MessageBoxButtons.Close);
            if (newfolder != null)
            {
                // Проверим что такого каталога ещё нет в списке каталогов для поиска дубликатов
                if (_dataModel.FoldersCollection.Any(Folder => Folder.FullName == newfolder.Path))
                {
                    MsgBox.Message = "Folder\n" + newfolder.Path + "\nalready in the folders list.";
                    await MsgBox.Show();
                    return;
                }
                // Проверим что добавляемый каталог не являеся подкаталогом каталога уже присутствующего в списке
                foreach (Folder folder in _dataModel.FoldersCollection)
                {
                    if (newfolder.Path.StartsWith(folder.FullName))
                    {
                        string deepestfoldername = newfolder.Path.Substring(folder.FullName.Length);
                        if (deepestfoldername.StartsWith("\\"))
                        {
                            MsgBox.Message = "Directory\n" + newfolder.Path + "\n is a subdirectory of directory\n" + folder.FullName + "\nYou can't create list of directories containing directory and its subdirectory.";
                            await MsgBox.Show();
                            return;
                        }
                    }
                }
                // Проверим что ни один из каталогов присутствующих в списке не являеся подкаталогом добавляемого каталога
                foreach (Folder folder in _dataModel.FoldersCollection)
                {
                    if (folder.FullName.StartsWith(newfolder.Path))
                    {
                        string deepestfoldername = folder.FullName.Substring(newfolder.Path.Length);
                        if (deepestfoldername.StartsWith("\\"))
                        {
                            MsgBox.Message = "Directory\n" + newfolder.Path + "\n is a directory containing directory already present in the directory list.\nYou can't create list of directories containing directory and its subdirectory.";
                            await MsgBox.Show();
                            return;
                        }
                    }
                }
                String AccessToken = StorageApplicationPermissions.FutureAccessList.Add(newfolder);
                _dataModel.FoldersCollection.Add(new Folder(newfolder.Path, false, AccessToken));

                BtnStartSearchEnabled = (_dataModel.Status == DataModel.SearchStatus.Completed) ? true : false;
                
                EmptyContentMessageVisibility = Visibility.Collapsed;
            }
        }

        private void button_DelFolder_Tapped(object sender, TappedRoutedEventArgs e)
        {
            List<Folder> foldersBuffer = new List<Folder>();
            // Перенесём фолдеры подлежащие удалению в буферный список 
            foreach (Folder f in listview_Folders.SelectedItems)
                foldersBuffer.Add(f);
            // А теперь удалим из основного списка фоолдеры попавшие в буферный список
            foreach (Folder f in foldersBuffer)
            {
                StorageApplicationPermissions.FutureAccessList.Remove(f.AccessToken);
                _dataModel.FoldersCollection.Remove(f);
            }

            BtnDelFolderEnabled = false;
            // Если после удаления список фолдеров пуст то сделаем кнопку запуска поиска неактивной

            bool folderListEmpty = (listview_Folders.Items.Count > 0) ? false : true;
               
            BtnStartSearchEnabled = folderListEmpty?false:true;
            EmptyContentMessageVisibility = folderListEmpty?Visibility.Visible:Visibility.Collapsed;
        }


        private void button_DeleteSelectedFiles_Tapped(object sender, TappedRoutedEventArgs e)
        {

        }
        private async void button_StartSearch_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // Проверим что если один из каталогов помечен как Primary то в списке должен присутствовать 
            // хотя бы ещё один каталог 
            if (_dataModel.FoldersCollection.Count < 2 & _dataModel.PrimaryFolder != null)
            {
                MsgBox.SetButtons(MessageBoxButtons.Close);
                MsgBox.Message = _resldr.GetString("SinglePrimaryDirectory");
                await MsgBox.Show();
                return;
            }

            // Проверим что выбран хотя бы один атрибут файла для сравнения при поиске дубликатов
            if (!(_dataModel.FileCompareOptions.CheckName | _dataModel.FileCompareOptions.CheckSize |
                _dataModel.FileCompareOptions.CheckContent | _dataModel.FileCompareOptions.CheckCreationDateTime |
                _dataModel.FileCompareOptions.CheckModificationDateTime))
            {
                MsgBox.SetButtons(MessageBoxButtons.Close);
                MsgBox.Message = _resldr.GetString("NoFileCompareAttributesChecked");
                await MsgBox.Show();
                return;
            }
            // Проверим есть ли результат предыдущего поиска и если есть покажем предупреждающее сообщение
            if (_dataModel.ResultFilesCollection.Count > 0)
            {
                MsgBox.SetButtons(MessageBoxButtons.Yes | MessageBoxButtons.No);
                MsgBox.Message = _resldr.GetString("DuplicatedAlreadyFound");
                MessageBoxButtons pressedbutton = await MsgBox.Show();
                if (pressedbutton == MessageBoxButtons.No)
                    return;
            }
            // await NavigateToSearchResults();
            await _dataModel.StartSearch();
        }
        private async void button_CancelSearch_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await _dataModel.StopSearch();
        }
        private void button_SaveSettings_Tapped(object sender, TappedRoutedEventArgs e)
        {
            _dataModel.Settings.Save();
        }
        //---------------------------------------------------------------------------
        #region Where To Search Tab event handlers
        //---------------------------------------------------------------------------
        private void toggleswitch_SetPrimary_Toggled(object sender, RoutedEventArgs e)
        {
            Folder cf = (sender as ToggleSwitch).DataContext as Folder;

            if (cf == null)
                return;

            String CurrentFolderName = cf.FullName;
            // Если выбранный фолдер установлен как Primary  то сбросим флажок на остальных фолдерах 
            if (cf.IsPrimary)
            {
                _dataModel.PrimaryFolder = cf;
                foreach (Folder f in _dataModel.FoldersCollection)
                {
                    if (f.FullName != cf.FullName)
                        f.IsPrimary = false;
                }
            }
            else
            {
                _dataModel.PrimaryFolder = null;
            }
        }


        private void listvew_Folders_Tapped(object sender, TappedRoutedEventArgs e)
        {
            BtnDelFolderEnabled = (listview_Folders.SelectedItems.Count > 0 && _dataModel.Status == DataModel.SearchStatus.Completed) ? true : false;
        }

        #endregion

        //---------------------------------------------------------------------------
        #region  Search Results Tab event handlers
        //---------------------------------------------------------------------------
        private void checkbox_SelectGroup_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox cbx = sender as CheckBox;
            FilesGroup datacontext = cbx.DataContext as FilesGroup;
            foreach (File f in datacontext)
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
          //  if (_doNotRegroup)
          //  {
          //      _doNotRegroup = false;
          //      return;
          //  }

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

        private void listview_Duplicates_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (listview_Duplicates.SelectedItems.Contains(e.ClickedItem))
                listview_Duplicates.SelectedItems.Remove(e.ClickedItem);
            else
                listview_Duplicates.SelectedItems.Add(e.ClickedItem);
          //  _mainPage.UpdateDeleteSelectedFilesButton(listview_Duplicates.SelectedItems.Count);
        }

        #endregion

        //---------------------------------------------------------------------------
        #region Search Options Tab event handlers
        //---------------------------------------------------------------------------
        private void listbox_ResultGroupingModes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _dataModel.CurrentGroupMode = ((string)((ComboBox)sender).SelectedValue);
        }

        #endregion
    }


}
