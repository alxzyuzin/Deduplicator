using System;
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
using System.Threading.Tasks;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Deduplicator {
    public sealed partial class ApplicationViews : UserControl, INotifyPropertyChanged
    {
        [Flags]
        public enum CmdButtons
        {
            DelSelectedFiles = 1,
            StartSearch = 2,
            CancelSearch = 4,
            SaveSettings = 8,
            AddFolder = 16,
            DelFolder = 32
        }

        [Flags]
        public enum View: byte
        {
            WhereToSearch = 1,
            SearchOptions = 2,
            SearchResults = 4,
            Settings = 8
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public event EventHandler<View> ChangeViewRequested;
        private void RequesViewChange(View newView)
        {
            if (ChangeViewRequested != null)
                ChangeViewRequested(this, newView);

        }
        private ResourceLoader _resldr = new ResourceLoader();

        private GroupingAttribute m_currentGroupingAttribute = new GroupingAttribute();

        #region Properties

        #region Visibility properties
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
        #endregion

        #region Buttons state properties

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

        #endregion

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

        private string _emptyContentMessage = string.Empty;
        public string EmptyContentMessage
        {
            get { return _emptyContentMessage; }
            set { _emptyContentMessage = value; NotifyPropertyChanged("EmptyContentMessage"); }
        }

        private DataModel _dataModel = new DataModel();
        public DataModel AppData => _dataModel;
  
        #endregion

        public ApplicationViews()
        {
            InitializeComponent();

            DataContext = this;

            _dataModel.FileCompareOptions.PropertyChanged += OnFileCompareOptionsPropertyChanged;

            cb_ResGroping.SelectedIndex = 0;
            cb_OptGroping.SelectedIndex = 0;

            _dataModel.SearchStatusChanged += OnSearchStatusChanged;
            SizeChanged += (object sender, SizeChangedEventArgs e)=>{ lv_Duplicates.InternalWidth = this.ActualWidth; };
        }

        private async void OnFileCompareOptionsPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            
            if (e.PropertyName == "CheckName" || e.PropertyName == "CheckSize" ||
                e.PropertyName == "CheckCreationDateTime" || e.PropertyName == "CheckModificationDateTime" ||
                e.PropertyName == "CheckContent")
            {
                 // Если событие возникает в процессе отката изменений параметров то игнорируем его
                if (_dataModel.FileCompareOptions.IsRollBack)
                    return;

                if (_dataModel.DuplicatesCount == 0)
                {
                    // Если поиск дубликатов не выполнялся то просто сохраняем новые значения
                    _dataModel.FileCompareOptions.Commit();
                    return;
                }
 
                string msg = "Changing file compare options will discard current search results.\n Would you like to change file compare options and clear results of last duplicates search?";
                MsgBoxButton pressedButton = await DisplayMessage(msg, MsgBoxButton.Yes | MsgBoxButton.No, 200);
                switch (pressedButton)
                {
                    case MsgBoxButton.Yes:
                        _dataModel.FileCompareOptions.Commit();
                        _dataModel.ClearSearchResults();
                        _dataModel.InvalidateDuplicates();
                        break;
                    case MsgBoxButton.No:
                        _dataModel.FileCompareOptions.RollBack();
                        break;
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
            var folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
            folderPicker.FileTypeFilter.Add("*");
            StorageFolder newfolder = await folderPicker.PickSingleFolderAsync();
 
            if (newfolder != null)
            {
                // Проверим что такого каталога ещё нет в списке каталогов для поиска дубликатов
                if (_dataModel.Folders.Any(Folder => Folder.FullName == newfolder.Path))
                {
                    string msg = $"Folder\n {newfolder.Path}\nalready in the folders list.";
                    await DisplayMessage(msg, MsgBoxButton.Close, 180);
                    return;
                }
                // Проверим что добавляемый каталог не являеся подкаталогом каталога уже присутствующего в списке
                var sfolder = _dataModel.Folders.FirstOrDefault(f => 
                                            newfolder.Path.StartsWith(f.FullName) &&
                                            newfolder.Path.Substring(f.FullName.Length).StartsWith("\\"));
                if (sfolder != null)
                {
                    string msg = $"Directory\n{newfolder.Path}\n is a subdirectory of directory\n{sfolder.FullName}\nYou can't create list of directories containing directory and its subdirectory.";
                    await DisplayMessage(msg, MsgBoxButton.Close, 300);
                    return;
                }
                // Проверим что ни один из каталогов присутствующих в списке не являеся подкаталогом добавляемого каталога
                var pfolder = _dataModel.Folders.FirstOrDefault(f => 
                                                f.FullName.StartsWith(newfolder.Path) &&
                                                f.FullName.Substring(newfolder.Path.Length).StartsWith("\\"));
                if (pfolder != null)
                {
                    string msg = $"Directory\n{newfolder.Path}\n is a directory containing directory already present in the directory list.\nYou can't create list of directories containing directory and its subdirectory.";
                    await DisplayMessage(msg, MsgBoxButton.Close, 300);
                    return;
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
            // Проверим что выбран хотя бы один атрибут файла для сравнения при поиске дубликатов
            if (!(_dataModel.FileCompareOptions.CheckName || _dataModel.FileCompareOptions.CheckSize ||
                _dataModel.FileCompareOptions.CheckContent || _dataModel.FileCompareOptions.CheckCreationDateTime ||
                _dataModel.FileCompareOptions.CheckModificationDateTime))
            {
                await DisplayMessage(_resldr.GetString("NoFileCompareAttributesChecked"), MsgBoxButton.Close, 150);
                return;
            }
            // Проверим есть ли результат предыдущего поиска и если есть покажем предупреждающее сообщение
            if (_dataModel.DuplicatesCount > 0)
            {
                 MsgBoxButton  pressedbutton = await DisplayMessage(_resldr.GetString("DuplicatedAlreadyFound"),
                                                    MsgBoxButton.Yes | MsgBoxButton.No, 150);
                 if (pressedbutton == MsgBoxButton.No)
                    return;
            }

            DisableComandButtons();
            await _dataModel.StartSearch(_dataModel.FileCompareOptions.GrouppingAttributes);
            RequesViewChange(View.SearchResults);
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

        private async void ts_SetPrimary_Toggled(object sender, RoutedEventArgs e)
        {
            var toggledFolder = (sender as ToggleSwitch).DataContext as Folder;

            if (toggledFolder == null)
                return;

            // Если выбранный фолдер установлен как Primary  то сбросим флажок на остальных фолдерах 
            if (toggledFolder.IsPrimary)
            {
                // Проверим что если один из каталогов помечен как Primary то в списке должен присутствовать 
                // хотя бы ещё один каталог 
                if (_dataModel.FoldersCount < 2)
                {
                    MsgBox.SetButtons(MsgBoxButton.Close);
                    MsgBox.Message = _resldr.GetString("SinglePrimaryDirectory");
                    await MsgBox.Show();
                    toggledFolder.IsPrimary = false;
                    return;
                }
                foreach (Folder folder in _dataModel.Folders.Where(f=> f.FullName != toggledFolder.FullName))
                        folder.IsPrimary = false;
             }
         }

        private void ts_Protected_Toggled(object sender, RoutedEventArgs e)
        {
            if (_dataModel.DuplicatedFiles.Count>0)
                _dataModel.SetFilesProtection((sender as ToggleSwitch).DataContext as Folder,((ToggleSwitch)sender).IsOn);
        }

        private void lv_Folders_Tapped(object sender, TappedRoutedEventArgs e)
        {
            BtnDelFolderEnabled = (lv_Folders.SelectedItems.Count > 0 && _dataModel.OperationCompleted)? true : false;
        }

#endregion


#region  Search Results view event handlers

        private void cbx_SelectGroup_Checked(object sender, RoutedEventArgs e)
        {
            FilesGroup filesGroup = (sender as CheckBox).DataContext as FilesGroup;
            filesGroup.AddAllGroupFilesToSelectedItems(lv_Duplicates.SelectedItems);
            BtnDelFilesEnabled = lv_Duplicates.SelectedItems.Count > 0 ? true : false;
         }

        private void cbx_SelectGroup_Unchecked(object sender, RoutedEventArgs e)
        {
            FilesGroup filesGroup = (sender as CheckBox).DataContext as FilesGroup;
            filesGroup.RemoveAllGroupFilesFromSelectedItems(lv_Duplicates.SelectedItems);
            BtnDelFilesEnabled = lv_Duplicates.SelectedItems.Count > 0 ? true : false;
         }

        private void File_Tapped(object sender, TappedRoutedEventArgs e)
        {
            File clickedFile = (sender as Grid).DataContext as File;
            clickedFile.IsChecked = clickedFile.IsChecked ? false : true;
            BtnDelFilesEnabled = lv_Duplicates.SelectedItems.Count > 0 ? true : false;
        }

        private async void button_DeleteSelectedFiles_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var filesForDelete = new List<File>(lv_Duplicates.SelectedItems.Cast<File>().Where(f => !f.IsProtected));

            string res = await _dataModel.RemoveDuplicatedFile(filesForDelete);
            if (res != string.Empty)
                AppData.SearchStatusInfo = $"Error deleting file.{res}";
            else
                AppData.SearchStatusInfo = $"Selected files succefully deleted.";

            ResetGroupCheckBoxes(lv_Duplicates, false);
         }

        #endregion 

        private void Grouping_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedAttribute = ((ComboBox)sender).SelectedItem as GroupingAttribute;
            if (selectedAttribute != null && m_currentGroupingAttribute != selectedAttribute)
            {
                    m_currentGroupingAttribute = selectedAttribute;
                    if (_dataModel.DuplicatesCount != 0)
                    {
                        DisableComandButtons();
                        _dataModel.RegroupResultsByFileAttribute(selectedAttribute);
                    }
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

        private void lv_Duplicates_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ResetGroupCheckBoxes(sender);
        }

        private void ResetGroupCheckBoxes(object sender, bool? value = null)
        {
            foreach (ListViewHeaderItem headerItem in lv_Duplicates.ItemsPanelRoot.Children.Where(item => item.GetType().Name == "ListViewHeaderItem"))
            {
                var TemplateStackPanel = (headerItem.ContentTemplateRoot as Grid).Children[1] as StackPanel;
                var TemplateCheckBox = TemplateStackPanel.Children.First(item => item.GetType().Name == "CheckBox") as CheckBox;
                if (value == null)
                    TemplateCheckBox.IsChecked = (TemplateCheckBox.DataContext as FilesGroup).IsChecked;
                else
                    TemplateCheckBox.IsChecked = value;
            }
        }

        private async Task<MsgBoxButton> DisplayMessage(string message, MsgBoxButton button, int height)
        {
            MsgBox.SetButtons(button);
            MsgBox.Message = message;
            MsgBox.BoxHeight = height;
            return await MsgBox.Show();
        }

    } // Class ApplicationViews
} // Namespace Deduplicator
