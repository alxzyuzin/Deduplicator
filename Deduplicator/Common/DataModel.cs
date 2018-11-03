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
            OperationCanceled
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

        public enum SearchStatus { SelectingFiles,
            Sorting,
            SearchingDuplicates,
            GrouppingStarted,
            Groupping,
            GrouppingCompleted,
            Error,
            SearchCompleted,
            OperationCanceled,
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
        // Список каталогов в которых искать дубликаты
        public FolderColection FoldersCollection = new FolderColection();  
        // Первичный каталог (если определён)
        public Folder PrimaryFolder = null;
        // Список найденых дубликатов файлов сгруппированных по заданному аттрибуту
        public GroupedFilesCollection ResultFilesCollection = new GroupedFilesCollection();
        // Список файлов отобранных из каталогов в которых искать дубликаты    
        private ObservableCollection<File> FilesCollection = new ObservableCollection<File>();
        // Список файлов из первичного каталога
        private ObservableCollection<File> PrimaryFilesCollection = new ObservableCollection<File>();
        private CancellationTokenSource tokenSource = new CancellationTokenSource();

#region Fields
        private int _stage = 0;
        int _filesTotal = 0;    // общее количество кандидатов в дубликаты в текущей фазе очистки
        int _filesHandled = 0;  // количество кандидатов проанализированых к данному моменту
        string _currentStage = string.Empty;
        private ErrorData _error = new ErrorData("DataModel.cs");
#endregion

#region Properties
        SearchStatus _status = SearchStatus.JustInitialazed;
        public SearchStatus Status { get { return _status; } }

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

        private int _totalFilesHandled = 0; // Общее число файлов найденных в указанных каталогах
        
        private DateTime _startTime = DateTime.Now;

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

        public bool PrimaryFolderSelected { get; set; }

        public bool OperationCompleted
        {
            get
            {
                return _status == SearchStatus.Error ||
                       _status == SearchStatus.OperationCanceled ||
                       _status == SearchStatus.SearchCompleted ||
                       _status == SearchStatus.GrouppingCompleted ||
                       _status == SearchStatus.JustInitialazed
                       ? true : false;
            }
        }

#endregion

        public DataModel()
        {
        }

 
        public async Task StartSearch(FileSelectionOptions selectionOptions, List<FileAttribs> compareAttribsList)
        {
            FilesCollection.Clear();
            ResultFilesCollection.Clear();
            _totalFilesHandled = 0;
            _startTime = DateTime.Now;
            _stage = 0;

            Progress<SearchStatus>  status = new Progress<SearchStatus>(ReportStatus);
            CancellationToken token = tokenSource.Token;

            WorkItemHandler workhandler = delegate { Search(selectionOptions, compareAttribsList, status, token); };
            await ThreadPool.RunAsync(workhandler, WorkItemPriority.High, WorkItemOptions.TimeSliced);
        }

        /// <summary>
        /// Поиск дубликатов файлов
        /// </summary>
        /// <returns></returns>
        private async void Search(FileSelectionOptions selectionOptions, List<FileAttribs> compareAttribsList, 
                                  IProgress<SearchStatus> searchStatus, CancellationToken canselationToken)
        {
            _error.Set(ErrorType.OperationCanceled, "", 0, "");
            searchStatus.Report(SearchStatus.SelectingFiles);

            try
            {
                // Отберём файлы из заданных пользователем каталогов для дальнейшего анализа в FilesCollection
                foreach (Folder folder in FoldersCollection)
                    await GetFolderFiles(folder, FilesCollection, selectionOptions, searchStatus, canselationToken);

                // Если нашлись файлы подходящие под условия фильтра то выполняем среди них поиск дубликатов
                if (FilesCollection.Count > 0)
                {
                    FilesGroup group = new FilesGroup("All ungrouped files");
                    foreach (File file in FilesCollection)
                        group.Add(file);
                    ResultFilesCollection.Add(group);

                    searchStatus.Report(SearchStatus.SearchingDuplicates);
                    foreach (var attrib in compareAttribsList)
                    {
                        _stage++;
                        await SplitGroupsByAttribute(ResultFilesCollection, attrib, false, searchStatus, canselationToken);
                    }
                }
                if (PrimaryFolder != null)
                {

                    List<FilesGroup> groupsForDelete = new List<FilesGroup>();
                    File fileFromPrimaryFolder = null;
                    // Просматриваем все группы в результатах поиска и 
                    foreach (FilesGroup group in ResultFilesCollection)
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
                        ResultFilesCollection.Remove(group);
                }
                searchStatus.Report(SearchStatus.SearchCompleted);
            }
            catch (OperationCanceledException)
            {
                FilesCollection.Clear();
                ResultFilesCollection.Clear();

                if (_error.Type == ErrorType.OperationCanceled)
                    searchStatus.Report(SearchStatus.OperationCanceled);
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
        private async Task SplitGroupsByAttribute(GroupedFilesCollection filegroups, FileAttribs attribute, 
                                                    bool regrouping, IProgress<SearchStatus> status, CancellationToken canselationToken)
        {
            _filesTotal = 0;
            _filesHandled = 0;
            _currentStage = StageName(attribute);
            
            // Подсчитаем общее количество файлов подлежащих перегруппировке
            foreach (var group in filegroups)
                _filesTotal+=group.Count;

            status.Report(regrouping ? SearchStatus.GrouppingStarted : SearchStatus.ComparingStarted);

            GroupedFilesCollection groupsbuffer = new GroupedFilesCollection();
            try {
                 //Перенесём группы из исходного списка в буфер
                foreach (var group in filegroups)
                    groupsbuffer.Add(group);
                // Очистим исходный список групп 
                filegroups.Clear();

                // Если attribute == FileAttribs.None прсто собираем все файлы в одну группу
                if (attribute == FileAttribs.None)
                {
                    FilesGroup newgroup = new FilesGroup();
                    foreach (FilesGroup group in groupsbuffer)
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
                    // при несовпадении файлов удаляем из группы в буфере файлы, добавленные в новую группу
                    // Если количество файлов в новой группе больше 1, новую группу добавляем в список групп 
                    // с результатами поиска (если в группе только один файл значит он не является дубликатом)
                    // Процесс повторяем до те по пока в группе из буфера присутствуют файлы 

                    foreach (var group in groupsbuffer)
                    {
                        await QuickSortGroupByAttrib(group, 0, group.Count - 1, attribute);   // 10500 файлoв время 0.118s 

                        while (group.Count > 1)
                        {
                            FilesGroup newgroup = new FilesGroup();

                            newgroup.Add(group[0]);
                            for (int i = 0; i < group.Count - 1; i++)
                            {
                                canselationToken.ThrowIfCancellationRequested();
                                _filesHandled++;
                                status.Report(regrouping ? SearchStatus.Groupping : SearchStatus.Comparing);
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
                _error.Set(ErrorType.OperationCanceled, "", 0, "");
                throw new OperationCanceledException();
            }
            catch (Exception ex)
            {
                _error.Set(ErrorType.UnknownError,"SplitGroupsByAttribute", 350, ex.Message);
                throw new OperationCanceledException();
            }
            // Все группы обработаны, Почистим за собой сразу не дожидаясь сборщика мусора
            groupsbuffer.Clear();
        } 

        private void ReportStatus(SearchStatus searchStatus)
        {
            int totalDuplicatesCount = 0;
            _status = searchStatus;
            switch(searchStatus)
            {
                case SearchStatus.NewFileSelected:
                    SearchStatusInfo = string.Format(@"Selecting files to search duplicates. Selected {0} files. Total files found {1}. Total time taken {2}",
                                                 FilesCollection.Count,
                                                 _totalFilesHandled,
                                                (DateTime.Now - _startTime).ToString(@"hh\ \h\ \ mm\ \m\ \ ss\ \s\."));
                    break;
                case SearchStatus.Groupping:
                case SearchStatus.GrouppingStarted:
                    SearchStatusInfo = string.Format(@"Groupping files by {0}. Handled {1} files from {2}.", _currentStage, _filesHandled, _filesTotal);
                    break;
                case SearchStatus.GrouppingCompleted:
                    SearchStatusInfo = string.Format("Grouping complete. Regrouped {0} duplicates into {1} groups.", _filesTotal, ResultFilesCollection.Count);
                    break;

                case SearchStatus.Comparing:
                case SearchStatus.ComparingStarted:
                    SearchStatusInfo = string.Format(@"Comparing files by {0}. Compared {1} files from {2}.", _currentStage, _filesHandled, _filesTotal);
                    break;
                case SearchStatus.ComparingCompleted:
                    SearchStatusInfo = string.Format("Comparing complete. Found {0} duplicates into {1} groups.", _filesTotal, ResultFilesCollection.Count);
                    break;
                case SearchStatus.Sorting:
                    SearchStatusInfo = @"Sorting files";
                    break;
                case SearchStatus.SearchCompleted:
                    totalDuplicatesCount = 0;
                    foreach (FilesGroup g in ResultFilesCollection)
                        totalDuplicatesCount += g.Count;
                    SearchStatusInfo = string.Format(@"Search completed. Found {0} duplicates in {1} groups.",
                                                        totalDuplicatesCount, ResultFilesCollection.Count);
                    break;
                case SearchStatus.OperationCanceled:
                    SearchStatusInfo = string.Format(@"Operation canceled.");
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
        }

        public void CancelOperation()
        {
            ReportStatus(SearchStatus.StartCancelOperation);
            tokenSource.Cancel();
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
                        await GetFolderFiles(new Folder(item.Path, folder.IsPrimary, folder.SearchInSubfolders, folder.Protected ), 
                                            filelist, options, selectingFilesStatus, canselationToken);
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
        public async Task RegroupResultsByFileAttribute(FileAttribs attribute)
        {
            Progress<SearchStatus> status = new Progress<SearchStatus>(ReportStatus);
            CancellationToken token = tokenSource.Token;
            // Перенесём все найденные дубликаты сгруппированные ранее по какому либо признаку
            // в одну общую группу
            FilesGroup newgroup = new FilesGroup("All ungrouped files");
            foreach (FilesGroup group in ResultFilesCollection)
                foreach (File file in group)
                    newgroup.Add(file);
            ResultFilesCollection.Clear();
            ResultFilesCollection.Add(newgroup);
            // Разделим полученный ранее полный список дубликатов на группы по указанному атрибуту
            //_stopSearch = false;
            await SplitGroupsByAttribute(ResultFilesCollection, attribute, true, status, token);
            ResultFilesCollection.Invalidate();
            ReportStatus(SearchStatus.GrouppingCompleted);
        }
        
       public void ClearSearchResults()
        {
            FilesCollection.Clear();
            ResultFilesCollection.Clear();
            ResultFilesCollection.Invalidate();
            ReportStatus(SearchStatus.ResultsCleared);
        }

        string StageName(FileAttribs attrib)
        {
            switch(attrib)
            {
                case FileAttribs.Content: return "Content";
                case FileAttribs.DateCreated: return "Creation date time";
                case FileAttribs.DateModified: return "Modification date time";
                case FileAttribs.Name: return "Name";
                case FileAttribs.Size: return "Size";
                default: return "";
            }
        }
    }
}
