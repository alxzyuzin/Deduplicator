using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System.Threading;

namespace Deduplicator.Common
{

    public sealed class DataModel : INotifyPropertyChanged
    {
        public enum ErrorType
        {
            UnknownError,
            FileNotFound,
            OperationCanceled,
            SearchCanceled,
            RegroupingCanceled
        }
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

        public enum SearchStatus {
            SelectingFiles,
            Sorting,
            SearchingDuplicates,
            GroupingStarted,
            Grouping,
            GroupingCompleted,
            GroupingCanceled,
            Error,
            SearchCompleted,
            SearchCanceled,
            Stopping,
            NewFileSelected,
            ResultsCleared,
            UnDefined,
            JustInitialazed,
            ComparingStarted,
            Comparing,
            ComparingCompleted,
            StartCancelOperation
        }

        public event EventHandler<SearchStatus> SearchStatusChanged;
        private void NotifySearchStatusChanged(SearchStatus status)
        {
            if (SearchStatusChanged != null)
                SearchStatusChanged(this, status);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        // Первичный каталог (если определён)
        public Folder PrimaryFolder = null;


#region Fields
        // Список каталогов в которых искать дубликаты
        private ObservableCollection<Folder> _foldersCollection = new ObservableCollection<Folder>();
        // Список найденых дубликатов файлов сгруппированных по заданному аттрибуту
        private GroupedFilesCollection _resultFilesCollection = new GroupedFilesCollection();
        // Список файлов отобранных из каталогов в которых искать дубликаты    
        private ObservableCollection<File> FilesCollection = new ObservableCollection<File>();
        // Список файлов из первичного каталога
        private ObservableCollection<File> PrimaryFilesCollection = new ObservableCollection<File>();
        
        private int _totalFilesHandled = 0; // Общее число файлов найденных в указанных каталогах
        private DateTime _startTime = DateTime.Now;
        private int _filesTotal = 0;    // общее количество кандидатов в дубликаты в текущей фазе очистки
        private int _filesHandled = 0;  // количество кандидатов проанализированых к данному моменту
//        string _currentStage = string.Empty;
        private ErrorData _error = new ErrorData("DataModel.cs");

        private CancellationTokenSource _tokenSource;

        private GroupingAttribute _currentGroupingAttribute = null;
#endregion

#region Properties

        public ObservableCollection<Folder> Folders
        {
            get {return _foldersCollection;}
        }

        public GroupedFilesCollection DuplicatedFiles
        {
            get { return _resultFilesCollection; }
        }

        SearchStatus _status = SearchStatus.JustInitialazed;
        public SearchStatus Status { get { return _status; } }

        private string _searchStatusInfo = string.Empty;
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

        private int _foldersCount = 0;
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

        public int  DuplicatesCount { get { return _resultFilesCollection.Count; } }

        public bool PrimaryFolderSelected { get; set; }

        public bool OperationCompleted
        {
            get
            {
                return _status == SearchStatus.Error ||
                       _status == SearchStatus.SearchCanceled ||
                       _status == SearchStatus.SearchCompleted ||
                       _status == SearchStatus.GroupingCompleted ||
                       _status == SearchStatus.GroupingCanceled ||
                       _status == SearchStatus.JustInitialazed ||
                       _status == SearchStatus.ComparingCompleted ||
                       _status == SearchStatus.JustInitialazed
                       ? true : false;
            }
        }

        public Settings _settings = new Settings();
        public Settings Settings
        {
            get { return _settings; }
        }
#endregion

        public DataModel()
        {
            Settings.Restore();
        }

 
        public async Task StartSearch(FileSelectionOptions selectionOptions, ObservableCollection<GroupingAttribute> compareAttribsList)
        {
            FilesCollection.Clear();
            _resultFilesCollection.Clear();
            _totalFilesHandled = 0;
            _startTime = DateTime.Now;


            Progress<SearchStatus>  status = new Progress<SearchStatus>(ReportStatus);
            _tokenSource = new CancellationTokenSource();
            CancellationToken token = _tokenSource.Token;

            WorkItemHandler workhandler = delegate { Search(selectionOptions, compareAttribsList, status, token); };
            await ThreadPool.RunAsync(workhandler, WorkItemPriority.High, WorkItemOptions.TimeSliced);
        }

        /// <summary>
        /// Поиск дубликатов файлов
        /// </summary>
        /// <returns></returns>
        private async void Search(FileSelectionOptions selectionOptions, ObservableCollection<GroupingAttribute> compareAttribsList, 
                                  IProgress<SearchStatus> searchStatus, CancellationToken canselationToken)
        {
            _error.Set(ErrorType.OperationCanceled, "", 0, "");
            searchStatus.Report(SearchStatus.SelectingFiles);

            try
            {
                // Отберём файлы из заданных пользователем каталогов для дальнейшего анализа в FilesCollection
                foreach (Folder folder in _foldersCollection)
                    await GetFolderFiles(folder, FilesCollection, selectionOptions, searchStatus, canselationToken);

                // Если нашлись файлы подходящие под условия фильтра то выполняем среди них поиск дубликатов
                if (FilesCollection.Count > 0)
                {
                    FilesGroup group = new FilesGroup("All ungrouped files");
                    foreach (File file in FilesCollection)
                        group.Add(file);
                    _resultFilesCollection.Add(group);

                    searchStatus.Report(SearchStatus.SearchingDuplicates);
                    foreach (var attrib in compareAttribsList)
                    {
                        await SplitGroupsByAttribute(_resultFilesCollection, attrib, false, searchStatus, canselationToken);
                    }
                }
                // Дополнительно удалим из списка дубликатов файлы не дублирующие файлы из PrimaryFolder
                if (PrimaryFolder != null)
                {

                    List<FilesGroup> groupsForDelete = new List<FilesGroup>();
                    File fileFromPrimaryFolder = null;
                    // Просматриваем все группы в результатах поиска и 
                    foreach (FilesGroup group in _resultFilesCollection)
                    {
                        fileFromPrimaryFolder = null;
                        // Для каждого файла в группе проверяем его принадлежность к Primary folder
                        foreach (File file in group)
                        {
                            if (file.FromPrimaryFolder)
                            {
                                fileFromPrimaryFolder = file;
                                break;
                            }
                        }
                        // Если в группе находится файл принадлежащий к Primary folder
                        // Удаляем его из группы и называем группу по имени этого файла
                        if (fileFromPrimaryFolder != null)
                        {
                            group.Remove(fileFromPrimaryFolder);
                            group.Name = fileFromPrimaryFolder.Name;
                        }
                        else
                        { // если нет то помещаем группу в список для последующего удаления
                            groupsForDelete.Add(group);
                        }
                    }
                    // Удалим тз результатов поиска все группы не содержащие файлов из Primary folder
                    foreach (FilesGroup group in groupsForDelete)
                        _resultFilesCollection.Remove(group);
                }
                searchStatus.Report(SearchStatus.SearchCompleted);
            }
            catch (OperationCanceledException)
            {
                FilesCollection.Clear();
                _resultFilesCollection.Clear();

                if (_error.Type == ErrorType.SearchCanceled)
                    searchStatus.Report(SearchStatus.SearchCanceled);
                else
                    searchStatus.Report(SearchStatus.Error);
            }
        }

        /// <summary>
        /// Удаляет из коллекции файлы с уникальным значением указанног атрибута
        /// При задании сравнения файлов по одному атрибуту удаляет из коллекции файлов файлы с уникальным 
        /// значением атрибута, собирает файлы с совпадающим значением атрибута в группу и помещает созданную группу
        /// в коллекцию результатов поиска
        /// При задании сравнения файлов по нескольким атрибутам функуция вызывается отдельно для каждого атрибута 
        /// При каждом последующем вызове созданные при предыдущем вызове группы разбиваются на новые группы 
        /// в которых совпадает значение нового атрибута и удаляются файлы с уникальным значением атрибута в нутри группы
        /// Если значение переданного атрибута равно FileAttribs.None  - все группы объединяются в одну, 
        /// которая помещается в результаты поиска
        /// </summary>
        /// <param name="filegroups">Коллекция групп файлов</param>
        /// <param name="attribute">Атрибут значение которого проверяется на уникальность</param>
        private async Task SplitGroupsByAttribute(GroupedFilesCollection filegroups, GroupingAttribute attribute, 
                                                    bool regrouping, IProgress<SearchStatus> status, CancellationToken canselationToken)
        {
            _filesTotal = 0;
            _filesHandled = 0;
            _currentGroupingAttribute = attribute;
            //_currentStageName = attribute.Name;

            // Подсчитаем общее количество файлов подлежащих перегруппировке
            foreach (var group in filegroups)
                _filesTotal+=group.Count;

            status.Report(regrouping ? SearchStatus.GroupingStarted : SearchStatus.ComparingStarted);
            GroupedFilesCollection groupsBuffer = new GroupedFilesCollection();
            try {
                 //Перенесём группы из исходного списка в буфер
                foreach (var group in filegroups)
                    groupsBuffer.Add(group);
                // Очистим исходный список групп 
                filegroups.Clear();

                // Если attribute == FileAttribs.None прсто собираем все файлы в одну группу
                if (attribute.Attribute == FileAttribs.None)
                {
                    FilesGroup newgroup = new FilesGroup();
                    foreach (FilesGroup group in groupsBuffer)
                        foreach (File file in group)
                            newgroup.Add(file);
                    filegroups.Add(newgroup);
                }
                else
                {
                    // Для каждой группы в буфере 
                    // выполняем сортировку по текущему атрибуту файла
                    // Сравниваем файлы в группе по текущему атрибуту попарно первый со вторым второй с третьим и тд
                    // и при равенстве файлов добавляем файлы в новую группу
                    // при первом несовпадении файлов считаем формирование группы файлов совпадающих по заданному атрибуту
                    // завершенным.
                    // Удаляем из группы в буфере файлы, добавленные в новую группу.
                    // Если количество файлов в новой группе больше 1, новую группу добавляем в список групп 
                    // с результатами поиска (если в группе только один файл значит он не является дубликатом)
                    // Процесс повторяем до те по пока в группе из буфера присутствуют файлы 

                    foreach (var group in groupsBuffer)
                    {
                        await QuickSortGroupByAttrib(group, 0, group.Count - 1, attribute.Attribute); 

                        while (group.Count > 1)
                        {
                            FilesGroup newgroup = new FilesGroup();
                            newgroup.Add(group[0]);
                            for (int i = 0; i < group.Count - 1; i++)
                            {
                                canselationToken.ThrowIfCancellationRequested();
                                status.Report(regrouping ? SearchStatus.Grouping : SearchStatus.Comparing);
                                int compareResult = await group[i].CompareTo(group[i + 1], attribute.Attribute);
                                if (compareResult == 0)
                                    newgroup.Add(group[i + 1]);
                                else
                                    break;
                            }
                            // Удалим из обрабатываемой группы файлы, перенесённые в созданную группу
                            foreach (File file in newgroup)
                            {
                                newgroup.TotalSize += file.Size;
                                group.Remove(file);
                                ++_filesHandled;
                            }
                            //Сохраним новую группу в буфере результата
                            if (newgroup.Count > 1)
                               filegroups.Add(newgroup);
                        }
                    }
                }
             }
            catch (OperationCanceledException)
            {
                if (regrouping)
                    _error.Set(ErrorType.RegroupingCanceled, "", 0, "");
                else
                    _error.Set(ErrorType.SearchCanceled, "", 0, "");
              
                throw new OperationCanceledException();
            }
            catch (Exception ex)
            {
                _error.Set(ErrorType.UnknownError,"SplitGroupsByAttribute", 377, ex.Message);
                throw new OperationCanceledException();
            }
        } 

        private void ReportStatus(SearchStatus searchStatus)
        {
            int totalDuplicatesCount = 0;
            _status = searchStatus;
            switch(searchStatus)
            {
                case SearchStatus.NewFileSelected:
                    SearchStatusInfo = string.Format(@"Selecting files to search duplicates. Selected {0} files. Total files found {1}.",
                                                 FilesCollection.Count, _totalFilesHandled);
                    break;
                case SearchStatus.Grouping:
                case SearchStatus.GroupingStarted:
                    SearchStatusInfo = string.Format(@"Groupping files by {0}. Handled {1} files from {2}.",
                                                    _currentGroupingAttribute.Name, _filesHandled, _filesTotal);
                    break;
                case SearchStatus.GroupingCompleted:
                    SearchStatusInfo = string.Format("Grouping complete. Regrouped {0} duplicates into {1} groups.",
                                                      _filesTotal, _resultFilesCollection.Count);
                    break;

                case SearchStatus.Comparing:
                case SearchStatus.ComparingStarted:
                    SearchStatusInfo = string.Format(@"Comparing files by {0}. Compared {1} files from {2}.",
                                                    _currentGroupingAttribute.Name, _filesHandled, _filesTotal);
                    break;
                case SearchStatus.ComparingCompleted:
                    SearchStatusInfo = string.Format("Comparing complete. Found {0} duplicates into {1} groups.",
                                                    _filesTotal, _resultFilesCollection.Count);
                    break;
                case SearchStatus.Sorting:
                    SearchStatusInfo = @"Sorting files";
                    break;
                case SearchStatus.SearchCompleted:
                    totalDuplicatesCount = 0;
                    foreach (FilesGroup g in _resultFilesCollection)
                        totalDuplicatesCount += g.Count;
                    SearchStatusInfo = string.Format(@"Search completed. Found {0} duplicates in {1} groups.",
                                                        totalDuplicatesCount, _resultFilesCollection.Count);
                    break;
                case SearchStatus.SearchCanceled:
                    SearchStatusInfo = string.Format(@"Search canceled.");
                    break;
                case SearchStatus.GroupingCanceled:
                    SearchStatusInfo = string.Format(@"Grouping canceled.");
                    break;
                case SearchStatus.Error:
                    SearchStatusInfo = string.Format(@"Error in module {0}, function {1}, line {2}, message {3}.",
                                            _error.ModuleName, _error.FunctionName, _error.LineNumber, _error.Message);
                    break;
                 case SearchStatus.ResultsCleared:
                    SearchStatusInfo = string.Format(@"Search results cleared.");
                    break;
                case SearchStatus.StartCancelOperation:
                    SearchStatusInfo = string.Format(@"Canceling current operation.");
                    break;
                default:
                    SearchStatusInfo = string.Empty;
                    break;
            }
            NotifySearchStatusChanged(searchStatus);
            if (OperationCompleted)
                _resultFilesCollection.Invalidate();
        }

        public void CancelOperation()
        {
            ReportStatus(SearchStatus.StartCancelOperation);
            _tokenSource.Cancel();
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
        private async Task GetFolderFiles(Folder folder, ObservableCollection<File> filelist, FileSelectionOptions options,
                                            IProgress<SearchStatus> selectingFilesStatus, CancellationToken canselationToken)
        {
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
                _error.Set(ErrorType.FileNotFound, "GetFolderFiles", 423, e.Message);
                throw new OperationCanceledException();
            }

            foreach (IStorageItem item in folderitems)
            {
                canselationToken.ThrowIfCancellationRequested();
                _totalFilesHandled++;
                if (item.Attributes.HasFlag(FileAttributes.Directory))  
                {
                    if (folder.SearchInSubfolders)
                        await GetFolderFiles(new Folder(item.Path, folder.IsPrimary, folder.SearchInSubfolders, folder.IsProtected ), 
                                            filelist, options, selectingFilesStatus, canselationToken);
                }
                else
                {
                    try
                    {
                        string fileExtention = (item as StorageFile).FileType;
                        if (options.ExtentionRequested(fileExtention))
                        {
                            File file = new File(item.Name, item.Path, fileExtention, item.DateCreated.DateTime,
                                                    new DateTime(), 0, folder.IsPrimary, folder.IsProtected);
                        
                            Windows.Storage.FileProperties.BasicProperties basicproperties = await item.GetBasicPropertiesAsync();
                            file.DateModifyed = basicproperties.DateModified.DateTime;
                            file.Size = basicproperties.Size;
                            filelist.Add(file);
                            selectingFilesStatus.Report(SearchStatus.NewFileSelected);
                        }
                    }
                    catch(Exception e)
                    {
                        _error.Set(ErrorType.UnknownError, "GetFolderFiles", 515, e.Message);
                        throw new OperationCanceledException();
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
                int compareResult = await files[j].CompareTo(files[lastindex], compareattrib);
                if (compareResult <= 0)
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
        public async void RegroupResultsByFileAttribute(GroupingAttribute attribute)
        {
            if (_currentGroupingAttribute.Attribute == attribute.Attribute)
                return;

//            _currentGroupingAttribute = attribute;
            Progress<SearchStatus> status = new Progress<SearchStatus>(ReportStatus);
            _tokenSource = new CancellationTokenSource();
            CancellationToken token = _tokenSource.Token;

            WorkItemHandler workhandler = delegate { Regroup(_resultFilesCollection, attribute, true, status, token); };
            await ThreadPool.RunAsync(workhandler, WorkItemPriority.High, WorkItemOptions.TimeSliced);
            _resultFilesCollection.Invalidate();
        }
        
        private async void Regroup(GroupedFilesCollection filegroups, GroupingAttribute attribute,
                                                    bool regrouping, IProgress<SearchStatus> status, CancellationToken token)
        {
            GroupedFilesCollection rollbackGroupsBuffer = new GroupedFilesCollection();
            // Сохраним результаты предыдущей сортировки для восстановления в случае отката операции
            foreach (var group in _resultFilesCollection)
                rollbackGroupsBuffer.Add(group);
            // Перенесём все найденные дубликаты сгруппированные ранее по какому либо признаку
            // в одну общую группу
            FilesGroup newgroup = new FilesGroup("All ungrouped files");
            foreach (FilesGroup group in _resultFilesCollection)
                foreach (File file in group)
                    newgroup.Add(file);
            _resultFilesCollection.Clear();
            _resultFilesCollection.Add(newgroup);
            // Разделим полученный ранее полный список дубликатов на группы по указанному атрибуту
            try
            {
                await SplitGroupsByAttribute(_resultFilesCollection, attribute, true, status, token);
                status.Report(SearchStatus.GroupingCompleted);
            }
            catch (OperationCanceledException)
            {
                _resultFilesCollection.Clear();

                if (_error.Type == ErrorType.RegroupingCanceled)
                {
                    foreach (var group in rollbackGroupsBuffer)
                        _resultFilesCollection.Add(group);
                     status.Report(SearchStatus.GroupingCanceled);
                }
                else
                    status.Report(SearchStatus.Error);
            }
        }

       public void ClearSearchResults()
        {
            FilesCollection.Clear();
            _resultFilesCollection.Clear();
            ReportStatus(SearchStatus.ResultsCleared);
        }

        public void SetFilesProtection(Folder folder, bool isProtected)
        {
           foreach (var group in _resultFilesCollection)
           {
               foreach (File file in group)
               {
                   if (file.Path.StartsWith(folder.FullName))
                            file.IsProtected = isProtected;
               }
               // group.CollectionChanged();
            }

            _resultFilesCollection.Invalidate();
        }
    }
}
