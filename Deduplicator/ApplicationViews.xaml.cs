﻿using System;
using System.Collections.Generic;
using System.Linq;
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

namespace Deduplicator {
    public sealed partial class ApplicationViews : UserControl, INotifyPropertyChanged
    {
        [Flags]
        private enum CmdButtons
        {
            DelSelectedFiles = 1,
            StartSearch = 2,
            CancelSearch = 4,
            SaveSettings = 8,
            AddFolder = 16,
            DelFolder = 32
        }

        [Flags]
        public enum View {
            WhereToSearch = 1,
            SearchOptions = 2,
            SearchResults = 4,
            Settings = 8
        }

        private enum Operation {
            Search,
            Regroup
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        private ResourceLoader _resldr = new ResourceLoader();

        // Список аттрибутов по которым будет выполняться сравнение файлов при поиске дубликатов
        private FileCompareOptions _fileCompareOptions = new FileCompareOptions();
        // Критерии отбора файлов из заданных каталогов, среди которых будет выполняться поиск дубликатов
        private FileSelectionOptions _fileSelectionOptions = new FileSelectionOptions();

        

#region Properties
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

        private Visibility _viewWhereToSearchVisibility = Visibility.Collapsed;
        public Visibility ViewWhereToSearchVisibility
        {
            get { return _viewWhereToSearchVisibility; }
            set
            {
                if (_viewWhereToSearchVisibility != value)
                {
                    _viewWhereToSearchVisibility = value;
                    NotifyPropertyChanged("ViewWhereToSearchVisibility");
                }
            }
        }

        private Visibility _viewSearchResultsVisibility = Visibility.Collapsed;
        public Visibility ViewSearchResultsVisibility
        {
            get { return _viewSearchResultsVisibility; }
            set
            {
                if (_viewSearchResultsVisibility != value)
                {
                    _viewSearchResultsVisibility = value;
                    NotifyPropertyChanged("ViewSearchResultsVisibility");
                }
            }
        }

        private Visibility _viewSettingsVisibility = Visibility.Collapsed;
        public Visibility ViewSettingsVisibility
        {
            get { return _viewSettingsVisibility; }
            set
            {
                if (_viewSettingsVisibility != value)
                {
                    _viewSettingsVisibility = value;
                    NotifyPropertyChanged("ViewSettingsVisibility");
                }
            }
        }

        private Visibility _viewOptionsVisibility = Visibility.Collapsed;
        public Visibility ViewOptionsVisibility
        {
            get { return _viewOptionsVisibility; }
            set
            {
                if (_viewOptionsVisibility != value)
                {
                    _viewOptionsVisibility = value;
                    NotifyPropertyChanged("ViewOptionsVisibility");
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

        private Visibility _groupingSelectorVisibility = Visibility.Collapsed;
        public Visibility GroupingSelectorVisibility
        {
            get { return _groupingSelectorVisibility; }
            private set
            {
                if (_groupingSelectorVisibility != value)
                {
                    _groupingSelectorVisibility = value;
                    NotifyPropertyChanged("GroupingSelectorVisibility");
                    NotifyPropertyChanged("GroupingSelectorText");
                }
            }
        }

        private Visibility _groupByPrimaryFolderVisibility = Visibility.Collapsed;
        public Visibility GroupByPrimaryFolderVisibility
        {
            get { return _groupByPrimaryFolderVisibility; }
            private set
            {
                if (_groupByPrimaryFolderVisibility != value)
                {
                    _groupByPrimaryFolderVisibility = value;
                    NotifyPropertyChanged("GroupByPrimaryFolderVisibility");
                }
            }
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
            get { return _btnDelFolderEnabled; }
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

        private bool _btnDelFilesEnabled = false;
        public bool BtnDelFilesEnabled
        {
            get
            {
                return _btnDelFilesEnabled;

            }
            set
            {
                if (_btnDelFilesEnabled != value)
                {
                    _btnDelFilesEnabled = value;
                    NotifyPropertyChanged("BtnDelFilesEnabled");
                }
            }
        }


        private bool _groupingModeSelectorEnabled = true;
        public bool GroupingModeSelectorEnabled
        {
            get { return _groupingModeSelectorEnabled; }
            set
            {
                if (_groupingModeSelectorEnabled == value)
                {
                    _groupingModeSelectorEnabled = value;
                    NotifyPropertyChanged("GroupingModeSelectorEnabled");
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

        private DataModel _dataModel = new DataModel();
        public DataModel AppData
        {
            get {return _dataModel;}
        }

        public FileSelectionOptions FileSelectionOptions
        {
            get { return _fileSelectionOptions; }
        }

        public FileCompareOptions FileCompareOptions
        {
            get { return _fileCompareOptions; }
        }

 #endregion

        public ApplicationViews()
        {
            InitializeComponent();
            DataContext = this;

            FileSelectionOptions.AudioFileExtentions = _dataModel.Settings.AudioFileExtentions;
            FileSelectionOptions.ImageFileExtentions = _dataModel.Settings.ImageFileExtentions;
            FileSelectionOptions.VideoFileExtentions = _dataModel.Settings.VideoFileExtentions;

            FileCompareOptions.PropertyChanged += OnFileCompareOptionsPropertyChanged;
            FileCompareOptions.CurrentGroupModeIndex = 0;

            // Подпишемся на события модели данных
            _dataModel.SearchStatusChanged += OnSearchStatusChanged;
            this.SizeChanged += OnSizeChanged;

            lv_Duplicates.ItemClick += Lv_Duplicates_ItemClick;

        }

        private void Lv_Duplicates_ItemClick(object sender, ItemClickEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            lv_Duplicates.InternalWidth = this.ActualWidth;
        }

        private async void OnFileCompareOptionsPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            
            if (e.PropertyName == "CheckName" || e.PropertyName == "CheckSize" ||
                e.PropertyName == "CheckCreationDateTime" || e.PropertyName == "CheckModificationDateTime" ||
                e.PropertyName == "CheckContent")
            {
                 // Если событие возникает в процессе отката изменений параметров то игнорируем его
                if (FileCompareOptions.IsRollBack)
                    return;

                if (_dataModel.DuplicatesCount == 0)
                {
                    // Если поиск дубликатов не выполнялся то просто сохраняем новые значения
                    FileCompareOptions.Commit();
                }
                else
                {
                    MsgBox.SetButtons(MessageBoxButtons.Yes | MessageBoxButtons.No);
                    MsgBox.Message = "Changing file compare options will discard current search results.\n Would you like to change file compare options\n and clear results of last duplicates search?";
                    MessageBoxButtons pressedButton = await MsgBox.Show();
                    if (pressedButton == MessageBoxButtons.Yes)
                    {
                        FileCompareOptions.Commit();
                        _dataModel.ClearSearchResults();
                    }

                    if (pressedButton == MessageBoxButtons.No)
                    {
                        FileCompareOptions.RollBack();
                    }
                }
            }

        }

        private void OnSearchStatusChanged(object sender, DataModel.SearchStatus status)
        {
  
            if (_dataModel.OperationCompleted)
            {
                BtnAddFolderEnabled = true;
                BtnDelFolderEnabled = (lv_Folders.SelectedItems.Count > 0) ? true : false;
                BtnStartSearchEnabled = (lv_Folders.Items.Count > 0) ? true : false;
                BtnStopSearchEnabled = false;
                GroupingModeSelectorEnabled = true;
            }
        }

        public void SwitchTo(View view)
        {

            ViewWhereToSearchVisibility = view.HasFlag(View.WhereToSearch) ? Visibility.Visible : Visibility.Collapsed;
            ViewOptionsVisibility       = view.HasFlag(View.SearchOptions) ? Visibility.Visible : Visibility.Collapsed;
            ViewSearchResultsVisibility = view.HasFlag(View.SearchResults) ? Visibility.Visible : Visibility.Collapsed;
            ViewSettingsVisibility      = view.HasFlag(View.Settings)      ? Visibility.Visible : Visibility.Collapsed;

            GroupingSelectorVisibility     = _dataModel.PrimaryFolder == null ? Visibility.Visible : Visibility.Collapsed;
            GroupByPrimaryFolderVisibility = _dataModel.PrimaryFolder == null ? Visibility.Collapsed : Visibility.Visible;

            EmptyContentMessage = string.Empty;

            switch (view)
            {
                case View.WhereToSearch:
                    TabHeader = "List of folders where to search duplicates.";
                    CmdButtonsVisualState = CmdButtons.AddFolder|CmdButtons.DelFolder|CmdButtons.StartSearch|CmdButtons.CancelSearch;
                    EmptyContentMessage = "No folders selected for searching duplicated files.\n Add folders where search duplicates.";
                    break;
                case View.SearchOptions:
                    TabHeader = "Search options.";
                    CmdButtonsVisualState = CmdButtons.StartSearch | CmdButtons.CancelSearch;
                    break;
                case View.SearchResults:
                    TabHeader = "Search results.";
                    CmdButtonsVisualState = CmdButtons.DelSelectedFiles | CmdButtons.StartSearch | CmdButtons.CancelSearch;
                    EmptyContentMessage = "No duplicates found.";
                    break;
                case View.Settings:
                    TabHeader = "Settings.";
                    CmdButtonsVisualState = CmdButtons.SaveSettings;
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
                if (_dataModel.Folders.Any(Folder => Folder.FullName == newfolder.Path))
                {
                    MsgBox.Message = "Folder\n" + newfolder.Path + "\nalready in the folders list.";
                    await MsgBox.Show();
                    return;
                }
                // Проверим что добавляемый каталог не являеся подкаталогом каталога уже присутствующего в списке
                foreach (Folder folder in _dataModel.Folders)
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
                foreach (Folder folder in _dataModel.Folders)
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
                _dataModel.Folders.Add(new Folder(newfolder.Path, false, AccessToken));

                BtnStartSearchEnabled = (_dataModel.OperationCompleted) ? true : false;
                
                EmptyContentMessageVisibility = Visibility.Collapsed;
            }
        }

        private void button_DelFolder_Tapped(object sender, TappedRoutedEventArgs e)
        {
            List<Folder> foldersBuffer = new List<Folder>();
            // Перенесём фолдеры подлежащие удалению в буферный список 
            foreach (Folder f in lv_Folders.SelectedItems)
                foldersBuffer.Add(f);
            // А теперь удалим из основного списка фолдеры попавшие в буферный список
            foreach (Folder f in foldersBuffer)
            {
                StorageApplicationPermissions.FutureAccessList.Remove(f.AccessToken);
                _dataModel.Folders.Remove(f);
            }

            BtnDelFolderEnabled = false;
            // Если после удаления список фолдеров пуст то сделаем кнопку запуска поиска неактивной
            BtnStartSearchEnabled = (lv_Folders.Items.Count > 0) ? true : false;
            EmptyContentMessageVisibility = (lv_Folders.Items.Count > 0) ? Visibility.Collapsed:Visibility.Visible;
        }

        private async void button_StartSearch_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // Проверим что если один из каталогов помечен как Primary то в списке должен присутствовать 
            // хотя бы ещё один каталог 
            if (_dataModel.Folders.Count < 2 & _dataModel.PrimaryFolder != null)
            {
                MsgBox.SetButtons(MessageBoxButtons.Close);
                MsgBox.Message = _resldr.GetString("SinglePrimaryDirectory");
                await MsgBox.Show();
                return;
            }

            // Проверим что выбран хотя бы один атрибут файла для сравнения при поиске дубликатов
            if (!(FileCompareOptions.CheckName | FileCompareOptions.CheckSize |
                FileCompareOptions.CheckContent | FileCompareOptions.CheckCreationDateTime |
                FileCompareOptions.CheckModificationDateTime))
            {
                MsgBox.SetButtons(MessageBoxButtons.Close);
                MsgBox.Message = _resldr.GetString("NoFileCompareAttributesChecked");
                await MsgBox.Show();
                return;
            }
            // Проверим есть ли результат предыдущего поиска и если есть покажем предупреждающее сообщение
            if (_dataModel.DuplicatesCount > 0)
            {
                 MsgBox.SetButtons(MessageBoxButtons.Yes | MessageBoxButtons.No);
                 MsgBox.Message = _resldr.GetString("DuplicatedAlreadyFound");
                 MessageBoxButtons pressedbutton = await MsgBox.Show();
                if (pressedbutton == MessageBoxButtons.No)
                    return;
            }

            DisableComandButtons();
       //     await _dataModel.StartSearch(FileSelectionOptions, FileCompareOptions.GrouppingAttributes);
        }

        private void button_CancelSearch_Tapped(object sender, TappedRoutedEventArgs e)
        {
            BtnStopSearchEnabled = false;
            _dataModel.CancelOperation();
        }

        private void button_SaveSettings_Tapped(object sender, TappedRoutedEventArgs e)
        {
            _dataModel.Settings.Save();
        }

#region Where To Search view event handlers

        private void toggleswitch_SetPrimary_Toggled(object sender, RoutedEventArgs e)
        {
            Folder cf = (sender as ToggleSwitch).DataContext as Folder;

            if (cf == null)
                return;

            // Если выбранный фолдер установлен как Primary  то сбросим флажок на остальных фолдерах 
            if (cf.IsPrimary)
            {
                _dataModel.PrimaryFolder = cf;
                foreach (Folder f in _dataModel.Folders)
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

        private void lv_Folders_Tapped(object sender, TappedRoutedEventArgs e)
        {
            BtnDelFolderEnabled = (lv_Folders.SelectedItems.Count > 0 && _dataModel.OperationCompleted)? true : false;
        }

#endregion


#region  Search Results view event handlers

        private void cbx_SelectGroup_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox cbx = sender as CheckBox;
            FilesGroup datacontext = cbx.DataContext as FilesGroup;
            foreach (File f in datacontext)
            {
                lv_Duplicates.SelectedItems.Add(f);
            }

            BtnDelFilesEnabled = lv_Duplicates.SelectedItems.Count > 0 ? true : false;
         }

        private void cbx_SelectGroup_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox cbx = sender as CheckBox;
            FilesGroup datacontext = cbx.DataContext as FilesGroup;
            foreach (File f in datacontext)
            {
                lv_Duplicates.SelectedItems.Remove(f);
            }
            BtnDelFilesEnabled = lv_Duplicates.SelectedItems.Count > 0 ? true : false;
         }

        private void lv_Duplicates_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (((ListView)sender).SelectedItems.Contains(e.ClickedItem))
                ((ListView)sender).SelectedItems.Remove(e.ClickedItem);
            else
                ((ListView)sender).SelectedItems.Add(e.ClickedItem);

            BtnDelFilesEnabled = ((ListView)sender).SelectedItems.Count > 0 ? true : false;
        }

        private async void button_DeleteSelectedFiles_Tapped(object sender, TappedRoutedEventArgs e)
        {
            List<File> deletedFiles = new List<File>();
            foreach (File file in ((ListView)sender).SelectedItems)
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
                foreach (var group in _dataModel.DuplicatedFiles)
                {
                    if (group.Contains(file))
                        group.Remove(file);
                }
            }
        }


#endregion

        private void OptionsGroupingModes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
       //     cb_ResGroping.SelectedIndex = ((ComboBox)sender).SelectedIndex;
              RegroupResult((GroupingAttribute)((ComboBox)sender).SelectedItem);
        }

        private void ResultGroupingModes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
       //     cb_OptGroping.SelectedIndex = ((ComboBox)sender).SelectedIndex;
              RegroupResult((GroupingAttribute)((ComboBox)sender).SelectedItem);
        }

        private void RegroupResult(GroupingAttribute selectedItem)
        {
            if (selectedItem != null && _dataModel.DuplicatesCount != 0)
            {
       //         FileAttribs attribute = _dataModel.FileAttributeFromName(selectedItem.ToString());
               DisableComandButtons();
                _dataModel.RegroupResultsByFileAttribute(selectedItem.Attribute);
            }
        }

        private void DisableComandButtons()
        {
            BtnAddFolderEnabled = false;
            BtnDelFolderEnabled = false;
            BtnStartSearchEnabled = false;
            BtnStopSearchEnabled = true;
            GroupingModeSelectorEnabled = false;
        }

    } // Class ApplicationViews
} // Namespace Deduplicator
