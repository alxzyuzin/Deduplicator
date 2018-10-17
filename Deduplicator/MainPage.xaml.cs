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
            //DelSelectedFiles = 16,
            //StartSearch = 32,
            //CancelSearch = 64,
            //SaveSettings = 128,
            //AddFolder = 256,
            //DelFolder = 512
        }

        private ResourceLoader _resldr = new ResourceLoader();


        //public static MainPage Current;
        //public event EventHandler ScenarioLoaded;
        //public event TappedEventHandler ButtonTapped;
        //private void NotyfyButtonTapped(object sender,TappedRoutedEventArgs e)
        //{
        //    if (ButtonTapped!=null)
        //        ButtonTapped(sender, e);
        //}
        //private Frame HiddenFrame = null;

        //private DataModel _dataModel = null;
        //public DataModel DataModel { get { return _dataModel; } }

        //private bool _firstloading = true;


        ApplicationTabs appTabs = new ApplicationTabs();


        public MainPage()
        {
 //           _dataModel = new DataModel(this);

            this.InitializeComponent();

            WorkArea.Child = appTabs;

            appTabs.SwitchTo(ApplicationTabs.Tabs.WhereToSearch);
            // This is a static public property that will allow downstream pages to get 
            // a handle to the MainPage instance in order to call methods that are in this class.
            //Current = this;

            // This frame is hidden, meaning it is never shown.  It is simply used to load
            // each application page and then pluck out the work area and
            // place them into the UserControls on the main page.
            //HiddenFrame = new Frame();
            //HiddenFrame.Visibility = Visibility.Collapsed;


            //ContentRoot.Children.Add(HiddenFrame);

            //this.ContentRoot.DataContext = _dataModel;

            //_dataModel.PropertyChanged += OnDatamodel_PropertyChanged;
            //_dataModel.SearchStarted += OnDatamodel_SearchStarted;
            //_dataModel.SearchCompleted += OnDatamodel_SearchCompleted;
            //_dataModel.SearchInterruptStarted += OnDatamodel_SearchInterruptStarted;
            //_dataModel.SearchInterruptCompleted += OnDatamodel_SearchInterruptCompleted;
            //_dataModel.Error += OnDatamodel_Error;

            //this.Unloaded += MainPage_Unloaded;
            //this.Loaded += MainPage_Loaded;
        }

        //private void MainPage_Loaded(object sender, RoutedEventArgs e)
        //{
        //    //WorkArea.Child = new ApplicationTabs();
        //    //await NavigateToSearchLocation();
        //}

        private void MainPage_Unloaded(object sender, RoutedEventArgs e)
        {
            StorageApplicationPermissions.FutureAccessList.Clear();
        }

        //private async void OnDatamodel_Error(object sender, Error e)
        //{
        //    switch (e.Type)
        //    {
        //        case ErrorType.UnknownError:
        //            MsgBox.SetButtons(MessageBoxButtons.Close);
        //            MsgBox.Message = string.Format(CultureInfo.CurrentCulture, _resldr.GetString("UnknownError"), e.Message);
        //            break;

        //        case ErrorType.FileNotFound:
        //            MsgBox.SetButtons(MessageBoxButtons.Close);
        //            MsgBox.Message = string.Format(CultureInfo.CurrentCulture, _resldr.GetString("FolderRemoved"), e.Message) ;
        //            break;
        //    }
        //    await MsgBox.Show();
        //}

        //private void OnDatamodel_SearchStarted(object sender, EventArgs e)
        //{
        //    button_CancelSearch.IsEnabled = true;
        //    button_StartSearch.IsEnabled = false;
        //}

        //        private void OnDatamodel_SearchCompleted(object sender, EventArgs e)
        //        {
        //            button_CancelSearch.IsEnabled = false;
        //            button_StartSearch.IsEnabled = true;

        ////           if (_dataModel.ResultFilesCollection.Count == 0)
        ////                _dataModel.SearchStatus = _resldr.GetString("SearchCompletedWithNoDuplicates");
        ////            else
        ////                _dataModel.SearchStatus = string.Format(_resldr.GetString("SearchCompletedWithDuplicates"),
        ////                                                        _dataModel.TotalDuplicatesCount, 
        ////                                                        _dataModel.ResultFilesCollection.Count);
        //        }

        //private void OnDatamodel_SearchInterruptStarted(object sender, EventArgs e)
        //{
        //    button_CancelSearch.IsEnabled = false;
        //}

        //private void OnDatamodel_SearchInterruptCompleted(object sender, EventArgs e)
        //{
        //    button_StartSearch.IsEnabled = true;
        //}

        //private void OnDatamodel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        //{
        //    if (e.PropertyName == "FoldersCount")
        //    {
        //        button_StartSearch.IsEnabled = (_dataModel.FoldersCount == 0)?false:true;
        //    }
        //}

        private void button_SearchLocation_Tapped(object sender, TappedRoutedEventArgs e)
        {
            appTabs.SwitchTo(ApplicationTabs.Tabs.WhereToSearch);
            //WorkArea.Child = new ApplicationTabs();

            //await NavigateToSearchLocation();
        }

        private void button_SearchOptions_Tapped(object sender, TappedRoutedEventArgs e)
        {
            appTabs.SwitchTo(ApplicationTabs.Tabs.SearchOptions);
            //await NavigateToSearchOptions();
        }

        private void button_SearchResults_Tapped(object sender, TappedRoutedEventArgs e)
        {
            appTabs.SwitchTo(ApplicationTabs.Tabs.SearchResults);
            //await NavigateToSearchResults();
        }

        private void button_Settings_Tapped(object sender, TappedRoutedEventArgs e)
        {
            appTabs.SwitchTo(ApplicationTabs.Tabs.Settings);
            //await  NavigateToSettings();
        }

        /*
        private async Task NavigateToSearchLocation()
        {
            
            bool contentLoaded = false;

            if (WorkArea.Content == null)
                contentLoaded = await LoadWorkArea(typeof(WhereToSearch), "grid_WhereToSearch");

            if (!contentLoaded & ((Grid)WorkArea.Content).Name != "grid_WhereToSearch")
                contentLoaded = await LoadWorkArea(typeof(WhereToSearch), "grid_WhereToSearch");
         
            if (contentLoaded)
            { 
                button_SearchLocation.IsEnabled = false;
                button_SearchOptions.IsEnabled = true;
                button_SearchResults.IsEnabled = true;
                button_Settings.IsEnabled = true;
                button_StartSearch.IsEnabled = (_dataModel.FoldersCount > 0 & !_dataModel.SearchInProgress) ? true : false;
                button_CancelSearch.IsEnabled = _dataModel.SearchInProgress ? true : false; ;
                ShowButtons( CmdButtons.SearchLocation | CmdButtons.SearchOptions | CmdButtons.SearchResults
                    | CmdButtons.Settings | CmdButtons.AddFolder | CmdButtons.DelFolder | CmdButtons.StartSearch
                    | CmdButtons.CancelSearch);
            }
            
    }

    private async Task NavigateToSearchOptions()
        {
            
            if (((Grid)WorkArea.Content).Name != "grid_SearchOptions")
            {
                if (await LoadWorkArea(typeof(SearchOptions), "grid_SearchOptions"))
                {
                    button_SearchLocation.IsEnabled = true;
                    button_SearchOptions.IsEnabled = false;
                    button_SearchResults.IsEnabled = true;
                    button_Settings.IsEnabled = true;
                    button_StartSearch.IsEnabled = (_dataModel.FoldersCount > 0 & !_dataModel.SearchInProgress) ? true : false;
                    button_CancelSearch.IsEnabled = _dataModel.SearchInProgress ? true : false; ;
                    ShowButtons(CmdButtons.SearchLocation | CmdButtons.SearchOptions | CmdButtons.SearchResults
                    | CmdButtons.Settings | CmdButtons.StartSearch | CmdButtons.CancelSearch);
                }
            }
            
        }

        private async Task NavigateToSearchResults()
        {
            /*
            if (((Grid)WorkArea.Content).Name != "grid_SearchResults")
            {
                if (await LoadWorkArea(typeof(SearchResults), "grid_SearchResults"))
                {
                    button_SearchLocation.IsEnabled = true;
                    button_SearchOptions.IsEnabled = true;
                    button_SearchResults.IsEnabled = false;
                    button_Settings.IsEnabled = true;
                    button_StartSearch.IsEnabled = (_dataModel.FoldersCount > 0 & !_dataModel.SearchInProgress) ? true : false;
                    button_CancelSearch.IsEnabled = _dataModel.SearchInProgress ? true : false; 
                    ShowButtons(CmdButtons.SearchLocation | CmdButtons.SearchOptions | CmdButtons.SearchResults
                    | CmdButtons.Settings | CmdButtons.DelSelectedFiles | CmdButtons.StartSearch | CmdButtons.CancelSearch);

                    if (_dataModel.ResultFilesCollection.Count == 0)
                        _dataModel.SearchStatus = "";
                }
            }
            
        }

        private async Task NavigateToSettings()
        {
            
            if (((Grid)WorkArea.Content).Name != "grid_Options")
            {
                if (await LoadWorkArea(typeof(Options), "grid_Options"))
                {
                    button_SearchLocation.IsEnabled = true;
                    button_SearchOptions.IsEnabled = true;
                    button_SearchResults.IsEnabled = true;
                    button_Settings.IsEnabled = false;

                    ShowButtons(CmdButtons.SearchLocation | CmdButtons.SearchOptions | CmdButtons.SearchResults
                    | CmdButtons.Settings | CmdButtons.SaveSettings);
                }
            }
            
        }
        */

        //private async void button_AddFolder_Tapped(object sender, RoutedEventArgs e)
        //{
        //    FolderPicker folderPicker = new FolderPicker();
        //    folderPicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
        //    folderPicker.FileTypeFilter.Add("*");
        //    StorageFolder newfolder = await folderPicker.PickSingleFolderAsync();

        //    MsgBox.SetButtons(MessageBoxButtons.Close);
        //    if (newfolder != null)
        //    {
        //        // Проверим что такого каталога ещё нет в списке каталогов для поиска дубликатов
        //        if (_dataModel.FoldersCollection.Any(Folder => Folder.FullName == newfolder.Path))
        //        {
        //            MsgBox.Message = "Folder\n" + newfolder.Path + "\nalready in the folders list.";
        //            await MsgBox.Show();
        //            return;
        //        }
        //        // Проверим что добавляемый каталог не являеся подкаталогом каталога уже присутствующего в списке
        //        foreach (Folder folder in _dataModel.FoldersCollection)
        //        {
        //            if (newfolder.Path.StartsWith(folder.FullName))
        //            {
        //                string deepestfoldername = newfolder.Path.Substring(folder.FullName.Length);
        //                if (deepestfoldername.StartsWith("\\"))
        //                {
        //                    MsgBox.Message = "Directory\n" + newfolder.Path + "\n is a subdirectory of directory\n" + folder.FullName + "\nYou can't create list of directories containing directory and its subdirectory.";
        //                    await MsgBox.Show();
        //                    return;
        //                }
        //            }
        //        }
        //        // Проверим что ни один из каталогов присутствующих в списке не являеся подкаталогом добавляемого каталога
        //        foreach (Folder folder in _dataModel.FoldersCollection)
        //        {
        //            if (folder.FullName.StartsWith(newfolder.Path))
        //            {
        //                string deepestfoldername = folder.FullName.Substring(newfolder.Path.Length);
        //                if (deepestfoldername.StartsWith("\\"))
        //                {
        //                    MsgBox.Message = "Directory\n" + newfolder.Path + "\n is a directory containing directory already present in the directory list.\nYou can't create list of directories containing directory and its subdirectory.";
        //                    await MsgBox.Show();
        //                    return;
        //                }
        //            }
        //        }
        //        String AccessToken = StorageApplicationPermissions.FutureAccessList.Add(newfolder);
        //        _dataModel.FoldersCollection.Add(new Folder(newfolder.Path, false, AccessToken));
        //    }
        //}

        //private void button_DelFolder_Tapped(object sender, TappedRoutedEventArgs e)
        //{

        //    NotyfyButtonTapped(sender, e);
        //    button_DelFolder.IsEnabled = false;


        //    //button_DelFolder.IsEnabled = (listvew_Folders.SelectedItems.Count > 0) ? true : false;
        //}

        //private void button_DeleteSelectedFiles_Tapped(object sender, TappedRoutedEventArgs e)
        //{
        //    NotyfyButtonTapped(sender, e);
        //}

        //private async void button_StartSearch_Tapped(object sender, TappedRoutedEventArgs e)
        //{
        //    // Проверим что если один из каталогов помечен как Primary то в списке должен присутствовать 
        //    // хотя бы ещё один каталог 
        //    if (_dataModel.FoldersCollection.Count < 2 & _dataModel.PrimaryFolder != null)
        //    {
        //        MsgBox.SetButtons(MessageBoxButtons.Close);
        //        MsgBox.Message = _resldr.GetString("SinglePrimaryDirectory");
        //        await MsgBox.Show();
        //        return;
        //    }

        //    // Проверим что выбран хотя бы один атрибут файла для сравнения при поиске дубликатов
        //    if (!(_dataModel.FileCompareOptions.CheckName | _dataModel.FileCompareOptions.CheckSize |
        //        _dataModel.FileCompareOptions.CheckContent | _dataModel.FileCompareOptions.CheckCreationDateTime |
        //        _dataModel.FileCompareOptions.CheckModificationDateTime))
        //    {
        //        MsgBox.SetButtons(MessageBoxButtons.Close);
        //        MsgBox.Message = _resldr.GetString("NoFileCompareAttributesChecked");
        //        await MsgBox.Show();
        //        return;
        //    }
        //    // Проверим есть ли результат предыдущего поиска и если есть покажем предупреждающее сообщение
        //    if (_dataModel.ResultFilesCollection.Count > 0)
        //    {
        //        MsgBox.SetButtons(MessageBoxButtons.Yes | MessageBoxButtons.No);
        //        MsgBox.Message = _resldr.GetString("DuplicatedAlreadyFound");
        //        MessageBoxButtons pressedbutton = await MsgBox.Show();
        //        if (pressedbutton == MessageBoxButtons.No)
        //            return;
        //    }
        //   // await NavigateToSearchResults();
        //    await _dataModel.StartSearch();
        //}

        //private async void button_CancelSearch_Tapped(object sender, TappedRoutedEventArgs e)
        //{
        //    await _dataModel.StopSearch();
        //}

        //private void button_SaveSettings_Tapped(object sender, TappedRoutedEventArgs e)
        //{
        //    NotyfyButtonTapped(sender, e);
        //}

        //private async Task<bool> LoadWorkArea(Type workareatype, string workelementname)
        //{
        //    /*
        //    if (!_firstloading)
        //    {
        //        this.Hide.Begin();
        //        await Task.Delay(TimeSpan.FromMilliseconds(600));
        //        _firstloading = false;
        //    }
        //    // Load the WhereToSearchFolders.xaml file into the Frame.
        //    HiddenFrame.Navigate(workareatype, this);
        //    // Get the top element, the Page, so we can look up the elements
        //    // that represent the input and output sections of the ScenarioX file.
        //    Page hiddenPage = HiddenFrame.Content as Page;
        //    // Get work element.
        //    UIElement uielement = hiddenPage.FindName(workelementname) as UIElement;
        //    if (uielement == null)
        //    {
        //        // Malformed WorkArea section.
        //        MsgBox.SetButtons(MessageBoxButtons.Close);
        //        MsgBox.Message = string.Format(CultureInfo.CurrentCulture, _resldr.GetString("CanNotLoadPage"), workelementname);
        //        await MsgBox.Show();
        //        return false;
        //    }
        //    // Find the LayoutRoot which parents Workaria sections in the main page.
        //    Panel panel = hiddenPage.FindName("LayoutRoot") as Panel;
        //    if (panel != null)
        //    {
        //        // Get rid of the content that is currently in the loaded page.
        //        panel.Children.Remove(uielement);
        //        // Populate workArea with the newly loaded content.
        //        MsgBox.SetButtons(MessageBoxButtons.Close);
        //        this.Show.Begin();
        //        WorkArea.Content = uielement;
        //    }
        //    else
        //    {
        //        // Malformed Scenario file.
        //        MsgBox.Message = String.Format(_resldr.GetString("LayoutRootNotFound"), workelementname);

        //        MsgBox.SetButtons(MessageBoxButtons.Close);
        //    }
        //     // Fire the ScenarioLoaded event since we know that everything is loaded now.
        //    if (ScenarioLoaded != null)
        //    {
        //        ScenarioLoaded(this, new EventArgs());
        //    }
        //    return (WorkArea.Content == null) ? false : true;
        //    */
        //    return false;
        //}

        //public void UpdateDeleteSelectedFilesButton(int selecteditemscount)
        //{
        //   button_DelSelectedFiles.IsEnabled = (selecteditemscount > 0)?true:false;
        //}

        //public void button_DelFolderSetIsEnabled(bool isenabled)
        //{
        //    button_DelFolder.IsEnabled = isenabled;
        //}

        private void ShowButtons(CmdButtons buttons)
        {
            button_SearchLocation.Visibility   = buttons.HasFlag(CmdButtons.SearchLocation)? Visibility.Visible : Visibility.Collapsed;
            button_SearchOptions.Visibility    = buttons.HasFlag(CmdButtons.SearchOptions) ? Visibility.Visible : Visibility.Collapsed;
            button_SearchResults.Visibility    = buttons.HasFlag(CmdButtons.SearchResults) ? Visibility.Visible : Visibility.Collapsed;
            button_Settings.Visibility         = buttons.HasFlag(CmdButtons.Settings) ? Visibility.Visible : Visibility.Collapsed;
            //button_DelSelectedFiles.Visibility = buttons.HasFlag(CmdButtons.DelSelectedFiles) ? Visibility.Visible : Visibility.Collapsed;
            //button_StartSearch.Visibility      = buttons.HasFlag(CmdButtons.StartSearch) ? Visibility.Visible : Visibility.Collapsed;
            //button_CancelSearch.Visibility     = buttons.HasFlag(CmdButtons.CancelSearch) ? Visibility.Visible : Visibility.Collapsed;
            //button_SaveSettings.Visibility     = buttons.HasFlag(CmdButtons.SaveSettings) ? Visibility.Visible : Visibility.Collapsed;
            //button_AddFolder.Visibility        = buttons.HasFlag(CmdButtons.AddFolder) ? Visibility.Visible : Visibility.Collapsed;
            //button_DelFolder.Visibility        = buttons.HasFlag(CmdButtons.DelFolder) ? Visibility.Visible : Visibility.Collapsed;

        }
      }
}


