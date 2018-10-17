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
using Windows.Foundation;

namespace Deduplicator.Common
{
    public enum AppStatus {SearchOptions, SearchResults, Settings }
    public enum LastAction { Search, Group}

    


    public sealed class DataModel : INotifyPropertyChanged

    {

        private sealed class ErrorData
        {
            public ErrorType Type;
            public string FunctionName;
            public string ModuleName;
            public int LineNumber;
            public string Message;

            public ErrorData(string moduleName)
            {
                ModuleName = moduleName;
            }

            public void Set(ErrorType type, string functionName, int lineNumber, string message)
            {
                Type = type;
                FunctionName = functionName;
                LineNumber = lineNumber;
                Message = message;
            }
        }

        public enum SearchStatus { SelectingFiles, Sorting, SearchingDuplicates, Groupping, Error, Completed, Stopping, NewFileSelected }

        public event EventHandler<SearchStatus> SearchStatusChanged;
        private void NotifySearchStatusChanged(SearchStatus status)
        {
            if (SearchStatusChanged != null)
                SearchStatusChanged(this, status);
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
        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public FolderColection FoldersCollection = new FolderColection();
        public Folder PrimaryFolder = null;
        public FileCollection FilesCollection;
        public FileCollection PrimaryFilesCollection;
        public GroupedFilesCollection ResultFilesCollection;
        public FileCompareOptions FileCompareOptions = new FileCompareOptions();
        public FileSelectionOptions FileSelectionOptions = new FileSelectionOptions();
        public ObservableCollection<string> ResultGrouppingModes = new ObservableCollection<string>();
        public Settings Settings = new Settings();

        #region Fields

        private bool _searchInProgress = false;

        private MainPage _mainPage = null;
        private bool _stopSearch = false;
        private ErrorData _error = new ErrorData("DataModel.cs");
  
        private int _stage = 0;

        

        private IProgress<SearchStatus> _status;
        #endregion

        #region Properties
        public SearchStatus Status { get; set; } = SearchStatus.Completed;

        private string _searchStatusInfo;
        public string SearchStatusInfo
        { get
            { return _searchStatusInfo; }
            set
            {
                if (_searchStatusInfo != value)
                {
                    _searchStatusInfo = value;
                    NotifyPropertyChanged("SearchStatusInfo");
                }
            }
        }

//        private int _totalDuplicatesFound = 0;  // Общее число найденных дубликатов
        
        private int _totalFilesHandled = 0; // Общее число файлов найденных в указанных каталогах
        
//        private int _totalFilesSelected = 0; // Общее число файлов отобранных для поиска дубликатов
        
        private DateTime _startTime = DateTime.Now;
 
        public string CurrentGroupMode { get; set; } = string.Empty;

        int         _foldersCount = 0;
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

//        Windows.UI.Xaml.Visibility _NoFoldersSelectedVisibility = Windows.UI.Xaml.Visibility.Visible;
//        public Windows.UI.Xaml.Visibility textblock_NoFoldersSelectedVisibility
//        {
//            get
//            {
//                return _NoFoldersSelectedVisibility;
//            }
//            set
//            {
//                if (_NoFoldersSelectedVisibility!=value)
//                {
//                    _NoFoldersSelectedVisibility = value;
//                    NotifyPropertyChanged("textblock_NoFoldersSelectedVisibility");
//               }
//            }

//        }

        public bool PrimaryFolderSelected { get; set; }

        #endregion

        public DataModel( MainPage mainpage)
        {
            _mainPage = mainpage;
            
            FilesCollection = new FileCollection(mainpage);
            PrimaryFilesCollection = new FileCollection(mainpage);
            ResultFilesCollection = new GroupedFilesCollection(mainpage);

            _status = new Progress<SearchStatus>(ReportStatus);
            
            ResultGrouppingModes.Add("Do not group files.");
            Settings.Restore();

            FileSelectionOptions.AudioFileExtentions = Settings.AudioFileExtentions;
            FileSelectionOptions.ImageFileExtentions = Settings.ImageFileExtentions;
            FileSelectionOptions.VideoFileExtentions = Settings.VideoFileExtentions;

            FileCompareOptions.PropertyChanged += FileCompareOptionsChanged;
        }

        private void FileCompareOptionsChanged(object sender, PropertyChangedEventArgs e)
        {
            NotifyPropertyChanged("FileCompareOptions");
        }
 
        /// <summary>
        /// Поиск дубликатов файлов
        /// </summary>
        /// <returns></returns>
        private async void Search(IProgress<SearchStatus> status)
        {
            _status.Report(SearchStatus.SelectingFiles);

            // Отберём файлы из заданных пользователем каталогов для дальнейшего анализа в FilesCollection
            foreach (Folder folder in FoldersCollection)
                await GetFolderFiles(folder, FilesCollection, FileSelectionOptions);

            // Если нашлись файлы подходящие под условия фильтра то выполняем среди них поиск дубликатов
            if (FilesCollection.Count > 0)
            {
                _status.Report(SearchStatus.Groupping);
                FilesGroup group = new FilesGroup("All ungrouped files");
                foreach (File file in FilesCollection)
                    group.Add(file);
                ResultFilesCollection.Add(group);

                _status.Report(SearchStatus.SearchingDuplicates);
                foreach (var attrib in FileCompareOptions.CheckAttribsList)
                {
                    _stage++; 
                   await SplitGroupsByAttribute(ResultFilesCollection, attrib);
                }
            }
            if (PrimaryFolder != null)
            {
                _status.Report(SearchStatus.Groupping);
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
//            ResultFilesCollection.Invalidate();
            _status.Report(SearchStatus.Completed);
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
            // Подсчитаем общее количество файлов подлежащих перегруппировке
//// TO DO Можно складывать количества файлов в каждой группе а не перебирать все файлы во всех группах
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
                    {
                        _status.Report(SearchStatus.Stopping);
                        return;
                    }
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

                        
                   if (_stage == FileCompareOptions.CheckAttribsList.Count) // Выполнено сравнение по всем аттрибутам
                   { // Подсчитаем общее количество найденных дубликатов
                      int newFilesTotal = 0;
      //// TO DO Можно складывать количества файлов в каждой группе а не перебирать все файлы во всех группах
                        foreach (var g in filegroups)
                            foreach (File file in g)
                                newFilesTotal++;
                     //       currentduplicatestotal = string.Format("Found {0} duplicates in {1} group(s).", newFilesTotal, filegroups.Count);
                    }
                    _status.Report(SearchStatus.SearchingDuplicates);
                        //SearchStatus = string.Format("Searching duplicates. Stage {0} of {1}. Handled {2} files from {3}. {4} Total time taken {5}.",
                        //    _stage, FileCompareOptions.CheckAttribsList.Count, filesHandled, filesTotal, currentduplicatestotal,
                        //    (DateTime.Now - _startTime).ToString(@"hh\ \h\ \ mm\ \m\ \ ss\ \s\."));
                    }
                    handledgroups++;
                }
                // Все группы обработаны, Почистим за собой сразу не дожидаясь сборщика мусора
                groupsbuffer.Clear();
            }
            catch(Exception ex)
            {
                _stopSearch = true;
                _error.Set(ErrorType.UnknownError,"SplitGroupsByAttribute", 395, ex.Message);
                _status.Report(SearchStatus.Error);
                return;
            }
        } 

        public async Task StartSearch()
        {
            FilesCollection.Clear();
            ResultFilesCollection.Clear();
 
            _stopSearch = false;
            _totalFilesHandled = 0;

            _startTime = DateTime.Now;
            _stage = 0;

            WorkItemHandler workhandler = delegate { Search(_status); };
            await ThreadPool.RunAsync(workhandler, WorkItemPriority.High, WorkItemOptions.TimeSliced);
        }

        private void ReportStatus(SearchStatus searchStatus)
        {
            Status = searchStatus;
            switch(searchStatus)
            {
                case SearchStatus.NewFileSelected:
                    _searchInProgress = true;
                    SearchStatusInfo = string.Format(@"Selecting files to search duplicates. Selected {0} files. Total files found {1}. Total time taken {2}",
                                                 FilesCollection.Count,
                                                 _totalFilesHandled,
                                                (DateTime.Now - _startTime).ToString(@"hh\ \h\ \ mm\ \m\ \ ss\ \s\."));
                    break;
                case SearchStatus.Groupping:
                    _searchInProgress = true;
                    SearchStatusInfo = @"Groupping selected files";
                    break;
                case SearchStatus.Sorting:
                    _searchInProgress = true;
                    SearchStatusInfo = @"Sorting files";
                    break;
                case SearchStatus.SearchingDuplicates:
                    _searchInProgress = true;
                    SearchStatusInfo = @"Searching duplicates";
                    break;
                case SearchStatus.Completed:
                    _searchInProgress = false;
                    int totalDuplicatesCount = 0;
                    foreach (FilesGroup g in ResultFilesCollection)
                        totalDuplicatesCount += g.Count;
                    SearchStatusInfo = string.Format(@"Search completed. Found {0} duplicates in {1} groups.",
                                                        totalDuplicatesCount, ResultFilesCollection.Count);
                    break;
                case SearchStatus.Error:
                    _stopSearch = true;
                    SearchStatusInfo = string.Format(@"Error in module {0}, function {1}, line {2}, message {3}.",
                        _error.ModuleName, _error.FunctionName, _error.LineNumber, _error.Message);
                    break;
                default:
                    SearchStatusInfo = string.Empty;
                    break;
            }

            NotifySearchStatusChanged(searchStatus);
        }

        public async Task StopSearch()
        {

            if (!_searchInProgress)  // Если процесс не активен то сразу выходим
                return;
//            SearchStatus = "Canceling current search ...";
//            NotifySearchInterruptStarted();
            _stopSearch = true;
            do        // В цикле с интервалом 100 миллисекунд проверяем статус ппроцесса поиска пока он не закончится
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }
            while (_searchInProgress);

            FilesCollection.Clear();
            ResultFilesCollection.Clear();

            _stopSearch = false;
//            SearchStatus = "Current search canceled.";
//            NotifySearchInterruptCompleted();
        }

        /// <summary>
        /// Формирует список файлов содержащихся в указанном каталоге
        /// Файлы включаются в результирующий список если удовлетворяют условиям определённым в параметре options
        /// </summary>
        /// <param name="folder">
        /// каталог в котором искать файлы
        /// </param>
        /// <param name="filelist">
        /// Список найденных файлов
        /// </param>
        /// <param name="options">
        /// условия которым должен удовлетворять файл для включения в список файлов
        /// </param>
        /// <returns></returns>
        private async Task GetFolderFiles(Folder folder, FileCollection filelist, FileSelectionOptions options)
        {
            if (_stopSearch)  // Необходимо прекратить поиск
                return;
            IReadOnlyList<IStorageItem> folderitems = null;
            StorageFolder f;

            try
            {
               // Каталог может быть удалён после того как начался поиск дубликато
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
                            _status.Report(SearchStatus.NewFileSelected);
                        }
                    }
                    catch(Exception e)
                    {
                        Type t = e.GetType();
                        _stopSearch = true;
                        _error.Set(ErrorType.UnknownError, "GetFolderFiles", 522, e.Message);
                        _status.Report(SearchStatus.Error);
                        return;
                    }
                }
            }
        }



        /// <summary>
        /// Реализует алгоритм сортировки спика файлов по заданному аттрибуту
        /// </summary>
        /// <param name="files"></param>
        /// <param name="firstindex"></param>
        /// <param name="lastindex"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        private async Task QuickSortGroupByAttrib(FilesGroup files, int firstindex, int lastindex, FileAttribs option)
        {
            if (firstindex >= lastindex)
                return;
          
            int c = await QuickSortPass(files, firstindex, lastindex, option);
            await QuickSortGroupByAttrib(files, firstindex, c - 1, option);
            await QuickSortGroupByAttrib(files, c + 1, lastindex, option);
        }

        /// <summary>
        /// Одиночный проход сортировки
        /// </summary>
        /// <param name="files"></param>
        /// <param name="firstindex"></param>
        /// <param name="lastindex"></param>
        /// <param name="compareattrib"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Перегруппировывает результаты поиска дубликатов по заданному атрибуту
        /// </summary>
        /// <param name="attribute"></param>
        public async void RegroupResultsByFileAttribute(FileAttribs attribute)
        {
            // Перенесём все найденные дубликаты сгруппированные ранее по какому либо признаку
            // в одну общую группу
            FilesGroup newgroup = new FilesGroup("All ungrouped files");
            foreach (FilesGroup group in ResultFilesCollection)
                foreach (File file in group)
                    newgroup.Add(file);
            ResultFilesCollection.Clear();
            ResultFilesCollection.Add(newgroup);
            // Разделим полученный ранее полный список дубликатов на группы по указанному атрибуту
            await SplitGroupsByAttribute(ResultFilesCollection, attribute);
        }
        
        /// <summary>
        /// На основании списка опций для сравнения файлов на закладке "Options"
        /// формирует список атрибутов по которым можно сгруппировать результаты поиска дубликатов
        /// на закладке "Results"
        /// </summary>
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

        /// <summary>
        /// Преобразует строковое название аттрибута по которому сгруппирован результат  в тип FileAttribs
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
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
