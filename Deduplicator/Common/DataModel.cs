using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.Storage;
using System.Collections.Specialized;
using Windows.UI.Core;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.System.Threading;
using System.IO;

namespace Deduplicator.Common
{
    public enum AppStatus {SearchOptions, SearchResults, Settings }
    public enum LastAction { Search, Group}
    public sealed class DataModel : INotifyPropertyChanged

    {
        string _unknownErrorMessage = "Function: {0}\nModule: {1}\nLine:{2}\n{3}";


        public event EventHandler SearchStarted;
        private async void NotifySearchStarted()
        {
            if (SearchStarted != null)
            {
                await _mainPage.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, delegate { SearchStarted(this, null); });
            }
        }

        public event EventHandler SearchCompleted;
        private async void NotifySearchCompleted()
        {
            if (SearchCompleted != null)
            {
                await _mainPage.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, delegate { SearchCompleted(this, null); });
            }
        }

        public event EventHandler SearchInterruptStarted;
        private async void NotifySearchInterruptStarted()
        {
            if (SearchInterruptStarted != null)
            {
                await _mainPage.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, delegate { SearchInterruptStarted(this, null); });
            }
        }

        public event EventHandler SearchInterruptCompleted;
        private async void NotifySearchInterruptCompleted()
        {
            if (SearchInterruptCompleted != null)
            {
                await _mainPage.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, delegate { SearchInterruptCompleted(this, null); });
            }
        }

        public event EventHandler<Error> Error;
        private async void NotifyError(Error error)
        {
            if (Error != null)
            {
                await _mainPage.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, delegate { Error(this, error); });
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private async void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                //PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                await _mainPage.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, delegate { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); });
            }
        }

        public FolderColection        FoldersCollection = new FolderColection();
        public Folder                 PrimaryFolder = null;
        public FileCollection         FilesCollection;
        public FileCollection         PrimaryFilesCollection;
        public GroupedFilesCollection ResultFilesCollection;
        public FileCompareOptions     FileCompareOptions = new FileCompareOptions();
        public FileSelectionOptions   FileSelectionOptions = new FileSelectionOptions();
        public ObservableCollection<string> ResultGrouppingModes = new ObservableCollection<string>();
        public Settings Settings = new Settings();

        #region Fields

        private MainPage _mainPage = null;
        private bool _stopSearch = false;

        private DateTime _startTime = DateTime.Now;
        private int _stage = 0;
        private int _totalFilesHandled = 0;
        #endregion

        #region Properties
        public string CurrentGroupMode { get; set; } = string.Empty;

        public int  TotalDuplicatesCount
        {
            get
            {
                int totalDuplicatesCount = 0;
                foreach (FilesGroup g in ResultFilesCollection)
                    foreach (File f in g)
                        totalDuplicatesCount++;
                return totalDuplicatesCount;
            }
        }

        private string _totalTimeTaken;
        public string TotalTimeTaken
        { get { return _totalTimeTaken; } }

        int _foldersCount = 0;
        public int  FoldersCount
        {
            get { return _foldersCount; }
            set
            {
                if (_foldersCount!=value)
                {
                    _foldersCount = value;
                    NotifyPropertyChanged("FoldersCount");
                }
            }
        }
        public int  FilesCount { get { return ResultFilesCollection.Count; } }
        Windows.UI.Xaml.Visibility _NoFoldersSelectedVisibility = Windows.UI.Xaml.Visibility.Visible;
        public Windows.UI.Xaml.Visibility textblock_NoFoldersSelectedVisibility
        {
            get
            {
                return _NoFoldersSelectedVisibility;
            }
            set
            {
                if (_NoFoldersSelectedVisibility!=value)
                {
                    _NoFoldersSelectedVisibility = value;
                    NotifyPropertyChanged("textblock_NoFoldersSelectedVisibility");
                }
            }

        }

        public bool PrimaryFolderSelected
        { get; set; }

        /// <summary>
        /// Признак того что идёт процесс поска дубликатов
        /// </summary>
        private bool _searchInProgress = false;
        public bool SearchInProgress
        {
            get { return _searchInProgress; }
            set
            {
                if (_searchInProgress!=value)
                {
                    _searchInProgress = value;
                    NotifyPropertyChanged("SearchInProgress");
                    NotifyPropertyChanged("CanStartOrStop");
                }
            }
        }

        /// <summary>
        ///  Текстовое описание статуса процесса поиска
        /// </summary>
        private string _searchStatus = string.Empty;
        public string SearchStatus
        {
            get
            {
               return _searchStatus;
            }
            set
            {
                if (_searchStatus!= value)
                {
                    _searchStatus = value;
                    NotifyPropertyChanged("SearchStatus");
                }
            }
        }

  //      private int _completionPercent;
  //      public int CompletionPercent
  //      {
  //          get
  //          {
  //              return _completionPercent;
  //          }
  //          set
  //          {
  //              if (_completionPercent != value)
  //              {
  //                  _completionPercent = value;
  //                  NotifyPropertyChanged("CompletionPercent");
  //              }
  //          }
  //      }

        #endregion

        public DataModel( MainPage mainpage)
        {
            _mainPage = mainpage;
            
            FilesCollection = new FileCollection(mainpage);
            PrimaryFilesCollection = new FileCollection(mainpage);
            ResultFilesCollection = new GroupedFilesCollection(mainpage);
                    
            ResultGrouppingModes.Add("Do not group files.");
            Settings.Restore();

            FileSelectionOptions.AudioFileExtentions = Settings.AudioFileExtentions;
            FileSelectionOptions.ImageFileExtentions = Settings.ImageFileExtentions;
            FileSelectionOptions.VideoFileExtentions = Settings.VideoFileExtentions;

            SearchInProgress = false;

            FoldersCollection.CollectionChanged += OnFoldersCollectionChanged;
            FileCompareOptions.PropertyChanged += FileCompareOptionsChanged;
        }

        private void FileCompareOptionsChanged(object sender, PropertyChangedEventArgs e)
        {
            NotifyPropertyChanged("FileCompareOptions");
        }
 
        #region Internal events handlers

        private void OnFoldersCollectionChanged( object sender, NotifyCollectionChangedEventArgs args)
        {
            FoldersCount = FoldersCollection.Count;
            textblock_NoFoldersSelectedVisibility = (FoldersCollection.Count>0)? Windows.UI.Xaml.Visibility.Collapsed:Windows.UI.Xaml.Visibility.Visible;
        } 

        #endregion

        /// <summary>
        /// Поиск дубликатов файлов
        /// </summary>
        /// <returns></returns>
        private async void Search()
        {
      
            NotifySearchStarted();
            SearchStatus = "Selecting files...";
                // Отберём файлы из заданных пользователем каталогов для дальнейшего анализа в FilesCollection
                
            foreach (Folder folder in FoldersCollection)
                await GetFolderFiles(folder, FilesCollection, FileSelectionOptions);

            // Если нашлись файлы подходящие под условия фильтра то выполняем среди них поиск дубликатов
            if (FilesCollection.Count > 0)
            {
                SearchStatus = "Grouping files...";
                FilesGroup group = new FilesGroup("All ungrouped files");
                foreach (File file in FilesCollection)
                    group.Add(file);
                ResultFilesCollection.Add(group);

                SearchStatus = "Searching for duplicates...";
                foreach (var attrib in FileCompareOptions.CheckAttribsList)
                {
                    _stage++; 
                   await SplitGroupsByAttribute(ResultFilesCollection, attrib);
                }
            }
            if (PrimaryFolder != null)
            {
                SearchStatus = "Grouping duplicates by files in primary folder ...";
                List<FilesGroup> groupsForDelete = new List<FilesGroup>();
                File fileFromPrimaryFolder = null; 
                foreach (FilesGroup group in ResultFilesCollection)
                {
                    fileFromPrimaryFolder = null;
                    foreach (File file in group)
                    {
                        if (file.FromPrimaryFolder)
                        {
                            fileFromPrimaryFolder = file;
                            break;
                        }
                    }
                    if (fileFromPrimaryFolder!=null)
                    {
                        group.Remove(fileFromPrimaryFolder);
                        group.Name = fileFromPrimaryFolder.Name;
                    }
                    else
                    {
                        groupsForDelete.Add(group);
                    }
                }
                foreach (FilesGroup group in groupsForDelete)
                    ResultFilesCollection.Remove(group);
            }
            ResultFilesCollection.Invalidate();
            SearchInProgress = false;
            _totalTimeTaken =  (DateTime.Now - _startTime).ToString();
            NotifySearchCompleted();
        }

        /// <summary>
        /// Удаляет из коллекции файлы с уникальным значением атрибутв
        /// </summary>
        /// <param name="filegroups">Коллекция групп файлов</param>
        /// <param name="attribute">Атрибут значение которого проверяется на уникальность</param>
        private async Task SplitGroupsByAttribute(GroupedFilesCollection filegroups, FileAttribs attribute)
        {
            string currentduplicatestotal = string.Empty;
            int filesHandled = 0;
            int filesTotal = 0;
            
            foreach (var group in filegroups)
                foreach (File file in group)
                    filesTotal++;

            try {
                GroupedFilesCollection groupsbuffer = new GroupedFilesCollection();
                //Перенесём группы из исходного списка в буфер

                foreach (var group in filegroups)
                    groupsbuffer.Add(group);
                // Очистим исходный список групп 
                filegroups.Clear();

                int handledgroups = 0;

                foreach (var group in groupsbuffer)
                {
                    if (_stopSearch)  // Необходимо прекратить поиск
                        return;
                    //SearchStatus = "Searching duplicates. Stage {0} of {1}. Sorting...";
                    await QuickSortGroupByAttrib(group, 0, group.Count - 1, attribute);   // 10500 файлoв время 0.118s 
                    //SearchStatus = "Searching duplicates. Stage {0} of {1}. Delete non duplicates...";                                                                                          // Попарно сравниваем файлы в группе для поиска дубликатов
                    while (group.Count > 1)
                    {
                        FilesGroup newgroup = new FilesGroup();
                        newgroup.Add(group[0]);
                        for (int i = 0; i < group.Count - 1; i++)
                        {
                            if (await group[i].IsEqualTo(group[i + 1], attribute))
                                newgroup.Add(group[i + 1]);
                            else
                                break;
                        }
                        // Удалим из обрабатываемой группы файлы, перенесённые в созданную группу
                        foreach (File file in newgroup)
                        {
                            newgroup.TotalSize += file.Size;
                            group.Remove(file);
                            ++filesHandled;


                  }
                        //Сохраним новую группу в буфере результата
                        if (newgroup.Count > 1)
                        {
                            filegroups.Add(newgroup);
                            if (attribute == FileAttribs.Content)
                                filegroups.Invalidate();
                        }

                        
                        if (_stage == FileCompareOptions.CheckAttribsList.Count)
                        {
                            int newFilesTotal = 0;
                            foreach (var g in filegroups)
                                foreach (File file in g)
                                    newFilesTotal++;
                            currentduplicatestotal = string.Format("Found {0} duplicates in {1} group(s).", newFilesTotal, filegroups.Count);
                        }
                        
                        SearchStatus = string.Format("Searching duplicates. Stage {0} of {1}. Handled {2} files from {3}. {4} Total time taken {5}.",
                            _stage, FileCompareOptions.CheckAttribsList.Count, filesHandled, filesTotal, currentduplicatestotal, (DateTime.Now-_startTime) );


                    }
                    handledgroups++;
                }
                // Все группы обработаны, Почистим за собой сразу не дожидаясь сборщика мусора
                groupsbuffer.Clear();
            }
            catch(Exception ex)
            {
                _stopSearch = true;
                NotifyError(new Error(ErrorType.UnknownError,
                            string.Format(_unknownErrorMessage, "SplitGroupsByAttribute", "DataModel.cs", "377", ex.Message)));
                return;
            }
        } 

        public async Task StartSearch()
        {
            FilesCollection.Clear();
            ResultFilesCollection.Clear();
            SearchInProgress = true;
            _stopSearch = false;
            _totalFilesHandled = 0;

            _startTime = DateTime.Now;
            _stage = 0;
        

        WorkItemHandler workhandler = delegate { Search(); };
            await ThreadPool.RunAsync(workhandler, WorkItemPriority.High, WorkItemOptions.TimeSliced);
        }

        public async Task StopSearch()
        {

            if (!_searchInProgress)  // Если процесс не активен то сразу выходим
                return;
            SearchStatus = "Canceling current search ...";
            NotifySearchInterruptStarted();
            _stopSearch = true;
            do        // В цикле с интервалом 100 миллисекунд проверяем статус ппроцесса поиска пока он не закончится
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }
            while (_searchInProgress);

            FilesCollection.Clear();
            ResultFilesCollection.Clear();

            _stopSearch = false;
            SearchStatus = "Current search canceled.";
            NotifySearchInterruptCompleted();
        }

        private async Task GetFolderFiles(Folder folder, FileCollection filelist, FileSelectionOptions options)
        {
            

            if (_stopSearch)  // Необходимо прекратить поиск
                return;
            IReadOnlyList<IStorageItem> folderitems = null;
            StorageFolder f;

            try
            {
               f = await StorageFolder.GetFolderFromPathAsync(folder.FullName);
               folderitems = await f.GetItemsAsync();
            }
            catch (FileNotFoundException e)
            {
                _stopSearch = true;
                NotifyError(new Error(ErrorType.FileNotFound, "Folder \n" + folder + "\nnot found."));
                return;
            }

            foreach (IStorageItem item in folderitems)
            {
                _totalFilesHandled++;
                if (item.Attributes.HasFlag(FileAttributes.Directory))  
                {
                    if (folder.SearchInSubfolders)
                        await GetFolderFiles(new Folder(item.Path, folder.IsPrimary, folder.SearchInSubfolders, folder.Protected ), filelist, options);
                }
                else
                {
                    try
                    {
                        File file = new File(item.Name, item.Path, (item as StorageFile).FileType, item.DateCreated.DateTime,
                                   new DateTime(), 0, folder.IsPrimary, folder.Protected);
                        if (options.ExtentionRequested(file.Extention))
                        {
                            Windows.Storage.FileProperties.BasicProperties basicproperties = await item.GetBasicPropertiesAsync();
                            file.DateModifyed = basicproperties.DateModified.DateTime;
                            file.Size = basicproperties.Size;
                            filelist.Add(file);
                        }
                    }
                    catch(Exception e)
                    {
                        Type t = e.GetType();
                        _stopSearch = true;
                        NotifyError(new Error(ErrorType.UnknownError, 
                                    string.Format(_unknownErrorMessage, "GetFolderFiles", "DataModel.cs", "522", e.Message)));
                        return;
                    }
                    SearchStatus = string.Format("Selecting files to search duplicates. Selected {0} files. Total files found {1}. Total time taken {2}",
                                                 FilesCollection.Count.ToString(), _totalFilesHandled, (DateTime.Now - _startTime).ToString());
                }
            }
        }

        private async Task QuickSortGroupByAttrib(FilesGroup files, int firstindex, int lastindex, FileAttribs option)
        {
            if (firstindex >= lastindex)
                return;
          
            int c = await QuickSortPass(files, firstindex, lastindex, option);
            await QuickSortGroupByAttrib(files, firstindex, c - 1, option);
            await QuickSortGroupByAttrib(files, c + 1, lastindex, option);
        }

        private async Task<int> QuickSortPass(FilesGroup files, int firstindex, int lastindex, FileAttribs compareattrib)
        {
            int i = firstindex;

            for (int j = i; j <= lastindex; j++)
            {
                
                if (( await files[j].CompareTo(files[lastindex], compareattrib)) <= 0)
                {
                    File t = files[i];
                    files[i] = files[j];
                    files[j] = t;
                    i++;
                }
            }
            return i - 1;
        }

        public async void RegroupResultsByFileAttribute(FileAttribs attribute)
        {
            FilesGroup newgroup = new FilesGroup("All ungrouped files");
            foreach (FilesGroup group in ResultFilesCollection)
                foreach (File file in group)
                    newgroup.Add(file);
            ResultFilesCollection.Clear();
            ResultFilesCollection.Add(newgroup);
            await SplitGroupsByAttribute(ResultFilesCollection, attribute);
        }
        
        public void UpdateResultGruppingModesList()
        {
            ResultGrouppingModes.Clear();

            if (this.FileCompareOptions.CheckName)
                ResultGrouppingModes.Add("Name");
            if (this.FileCompareOptions.CheckSize)
               ResultGrouppingModes.Add("Size");

            if (this.FileCompareOptions.CheckContent)
                ResultGrouppingModes.Add("Content");

            if (this.FileCompareOptions.CheckCreationDateTime)
                ResultGrouppingModes.Add("Creation date time");

            if (this.FileCompareOptions.CheckModificationDateTime)
                ResultGrouppingModes.Add("Modification date time");
        }

        public static FileAttribs ConvertGroupingNameToFileAttrib(string name)
        {
            switch (name)
            {
                case "Name": return FileAttribs.Name; 
                case "Size": return FileAttribs.Size; 
                case "Content": return FileAttribs.Content; 
                case "Creation date time": return FileAttribs.DateCreated; 
                case "Modification date time": return FileAttribs.DateModified;
                default: return FileAttribs.None; 
            }
        }
    }
}
