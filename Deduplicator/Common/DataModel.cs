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
            StartCancelOperation,
            Analyse,
            OperationCanceled
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
        private GroupedFilesCollection m_DuplicatesCollection; // = new GroupedFilesCollection(progress);
        // Список файлов отобранных из каталогов в которых искать дубликаты    
        private FilesGroup FilesCollection;
        
        private DateTime _startTime = DateTime.Now;
        private ErrorData _error = new ErrorData("DataModel.cs");

        private CancellationTokenSource _tokenSource;
        private Progress<OperationStatus> m_progress;

#endregion

        #region Properties

        public  ObservableCollection<Folder> Folders
        {
            get {return _foldersCollection;}
        }

        public GroupedFilesCollection DuplicatedFiles
        {
            get { return m_DuplicatesCollection; }
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

        public int  DuplicatesCount { get { return m_DuplicatesCollection.Count; } }

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
            m_progress = new Progress<OperationStatus>(ReportStatus);
            m_progress.ProgressChanged += Status_ProgressChanged;
            m_DuplicatesCollection = new GroupedFilesCollection(m_progress);
            FilesCollection = new FilesGroup(m_progress);
        }
 
        public async Task StartSearch(FileSelectionOptions selectionOptions, ObservableCollection<GroupingAttribute> compareAttribsList)
        {
            _tokenSource = new CancellationTokenSource();
            CancellationToken token = _tokenSource.Token;

            WorkItemHandler workhandler = delegate { Search(selectionOptions, compareAttribsList, token); };
            await ThreadPool.RunAsync(workhandler, WorkItemPriority.High, WorkItemOptions.TimeSliced);
        }

        private void Status_ProgressChanged(object sender, OperationStatus e)
        {
            int i = 0;
        }

        /// <summary>
        /// Поиск дубликатов файлов
        /// </summary>
        /// <returns></returns>
        private async void Search( FileSelectionOptions selectionOptions, 
                                   ObservableCollection<GroupingAttribute> compareAttribsList, 
                                   CancellationToken cancelToken )
        {
            _error.Set(ErrorType.OperationCanceled, "", 0, "");
            IProgress<OperationStatus> progress = m_progress;
            OperationStatus status = new OperationStatus
            {
                 Id = SearchStatus.SelectingFiles 
            };

            FilesCollection.Clear();
            m_DuplicatesCollection.Clear();
            try
            {
                // Отберём файлы из заданных пользователем каталогов для дальнейшего анализа в FilesCollection
                foreach (Folder folder in _foldersCollection)
                    await GetFolderFiles(folder, FilesCollection, selectionOptions, cancelToken, status);

                //// Если нашлись файлы подходящие под условия фильтра то выполняем среди них поиск дубликатов
                if (FilesCollection.Count > 1)
                {
                    FilesGroup fg = new FilesGroup(m_progress);
                    foreach (File file in FilesCollection)
                        fg.Add(file);

                    m_DuplicatesCollection.Add(fg);

                    await m_DuplicatesCollection.RemoveNonDuplicates(compareAttribsList, cancelToken);
                }
                //// Дополнительно удалим из списка дубликатов файлы не дублирующие файлы из PrimaryFolder
                if (PrimaryFolder != null)
                    DeleteNonPrimaryFolderDuplicates();

                status.Id = SearchStatus.SearchCompleted;
                progress.Report(status);
            }
            catch (OperationCanceledException)
            {
                FilesCollection.Clear();
                m_DuplicatesCollection.Clear();

                if (_error.Type == ErrorType.SearchCanceled || _error.Type == ErrorType.OperationCanceled)
                {
                    status.Id = SearchStatus.SearchCanceled;
                    progress.Report(status);
                }
                else
                {
                    status.Id = SearchStatus.Error;
                    progress.Report(status);
                }
            }
        }

        private void DeleteNonPrimaryFolderDuplicates()
        {
            List<FilesGroup> groupsForDelete = new List<FilesGroup>();

            // Просматриваем все группы в результатах поиска  
            foreach (FilesGroup group in m_DuplicatesCollection)
            {
                List<File> primaryFolderFiles = new List<File>();
                // Для каждого файла в группе проверяем его принадлежность к Primary folder
                foreach (File file in group)
                {
                    if (file.FromPrimaryFolder)
                         primaryFolderFiles.Add(file);
                }
                // Если в группе находится хотя бы один файл принадлежащий к Primary folder
                // Удаляем его из группы и называем группу по имени этого файла
                if (primaryFolderFiles.Count>0)
                {
                    group.Name = primaryFolderFiles[0].Name;
                    foreach(File file in primaryFolderFiles)
                        group.Remove(file);
                }
                else
                { // если нет то помещаем группу в список для последующего удаления
                    groupsForDelete.Add(group);
                }
            }
            // Удалим тз результатов поиска все группы не содержащие файлов из Primary folder
            foreach (FilesGroup group in groupsForDelete)
                m_DuplicatesCollection.Remove(group);
        }

  
        private void ReportStatus(OperationStatus status)
        {
            _status = status.Id;
            switch(status.Id)
            {
                case SearchStatus.NewFileSelected:
                    SearchStatusInfo = string.Format("Selecting files. Total files selected {0}.", status.HandledItems);
                    break;
                case SearchStatus.Grouping:
                    SearchStatusInfo = string.Format(@"Groupping files by {0}. Handled {1} files from {2}.",
                                                    status.Stage, status.HandledItems, status.TotalItems);
                    break;
                case SearchStatus.GroupingCompleted:
                    SearchStatusInfo = string.Format("Grouping complete. Regrouped {0} duplicates into {1} groups.",
                                                      m_DuplicatesCollection.FilesCount, m_DuplicatesCollection.Count);
                    break;

                case SearchStatus.ComparingStarted:
                case SearchStatus.Comparing:
                    SearchStatusInfo = string.Format(@"Comparing files by {0}. Compared {1} files from {2}.",
                                                   status.Stage, status.HandledItems, status.TotalItems);
                    break;
                //                case SearchStatus.ComparingCompleted:
                //                    SearchStatusInfo = string.Format("Comparing complete. Found {0} duplicates into {1} groups.",
                //                                                    _filesTotal, m_DuplicatesCollection.Count);
                //                    break;
                case SearchStatus.Sorting:
                    SearchStatusInfo = @"Sorting files ...";
                    break;
                case SearchStatus.SearchCompleted:
                    SearchStatusInfo = string.Format(@"Search completed. Found {0} duplicates in {1} groups.",
                                                      m_DuplicatesCollection.FilesCount, m_DuplicatesCollection.Count);
                    break;
                //                case SearchStatus.SearchCanceled:
                //                    SearchStatusInfo = string.Format(@"Search canceled.");
                //                    break;
                //                case SearchStatus.GroupingCanceled:
                //                    SearchStatusInfo = string.Format(@"Grouping canceled.");
                //                    break;
                //                //case SearchStatus.Error:
                //                //    SearchStatusInfo = string.Format(@"Error in module {0}, function {1}, line {2}, message {3}.",
                //                //                            _error.ModuleName, _error.FunctionName, _error.LineNumber, _error.Message);
                //                //    break;
                case SearchStatus.ResultsCleared:
                    SearchStatusInfo = string.Format(@"Search results cleared.");
                    break;
                    //                case SearchStatus.StartCancelOperation:
                    //                    SearchStatusInfo = string.Format(@"Canceling current operation.");
                    //                    break;
                    //                case SearchStatus.Analyse:
                    //                    SearchStatusInfo = string.Format(@"Analyzing file {0} from {1}.", 1,2);
                    //                    break;
                    //                default:
                    //                    SearchStatusInfo = string.Empty;
                    //                    break;
            }
            NotifySearchStatusChanged(status.Id);
            if (OperationCompleted)
                m_DuplicatesCollection.Invalidate();
        }

        public void CancelOperation()
        {
            OperationStatus status = new OperationStatus
            {
                Id = SearchStatus.StartCancelOperation,
                TotalItems = 0,
                HandledItems = 0,
                Stage = @"Start canceling operation."
            };
            ReportStatus(status);
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
        private async Task GetFolderFiles(Folder folder, ObservableCollection<File> filelist,
                                            FileSelectionOptions options, CancellationToken canselationToken,
                                            OperationStatus status )
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
                _error.Set(ErrorType.FileNotFound, "GetFolderFiles", 482, e.Message);
                throw new OperationCanceledException();
            }

            IProgress<OperationStatus> progress = m_progress;
            foreach (IStorageItem item in folderitems)
            {
                canselationToken.ThrowIfCancellationRequested();
                ++status.TotalItems;
                if (item.Attributes.HasFlag(FileAttributes.Directory))  
                {
                    if (folder.SearchInSubfolders)
                        await GetFolderFiles(new Folder(item.Path, folder.IsPrimary, folder.SearchInSubfolders, folder.IsProtected ), 
                                            filelist, options, canselationToken, status);
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
                            status.Id = SearchStatus.NewFileSelected;
                            ++status.HandledItems;
                            progress.Report(status);
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
        /// Перегруппировывает результаты поиска дубликатов по заданному атрибуту
        /// </summary>
        /// <param name="attribute"></param>
        public async void RegroupResultsByFileAttribute(GroupingAttribute attribute)
        {
            _tokenSource = new CancellationTokenSource();
            CancellationToken token = _tokenSource.Token;

            WorkItemHandler workhandler = delegate { Regroup(attribute, token); };
            await ThreadPool.RunAsync(workhandler, WorkItemPriority.High, WorkItemOptions.TimeSliced);
            m_DuplicatesCollection.Invalidate();
        }



        private async void Regroup( GroupingAttribute attribute, CancellationToken token)
        {
            GroupedFilesCollection rollbackGroupsBuffer = new GroupedFilesCollection(m_progress);
            // Сохраним результаты предыдущей сортировки для восстановления в случае отката операции
            foreach (var group in m_DuplicatesCollection)
                rollbackGroupsBuffer.Add(group);
            // Разделим полученный ранее полный список дубликатов на группы по указанному атрибуту
            try
            {
                await m_DuplicatesCollection.RegroupDuplicates(attribute, token);
            }
            catch (OperationCanceledException)
            {
                m_DuplicatesCollection.Clear();

                if (_error.Type == ErrorType.RegroupingCanceled)
                {
                    // Восстановим результаты предыдущей сортировки
                    foreach (var group in rollbackGroupsBuffer)
                        m_DuplicatesCollection.Add(group);
                }
            }
        }

       public void ClearSearchResults()
        {
            FilesCollection.Clear();
            m_DuplicatesCollection.Clear();
            OperationStatus status = new OperationStatus  { Id = DataModel.SearchStatus.ResultsCleared };
            ReportStatus(status);
        }

        public void SetFilesProtection(Folder folder, bool isProtected)
        {
            GroupedFilesCollection newGroupCollection = new GroupedFilesCollection(m_progress);
            foreach (var group in m_DuplicatesCollection)
           {
                FilesGroup newGroup = new FilesGroup(group.Name);
                foreach (File file in group)
               {
                    if (file.Path.StartsWith(folder.FullName))
                            file.IsProtected = isProtected;
                    File newFile = file.Clone();
                    newGroup.Add(newFile);
               }
                newGroupCollection.Add(newGroup);
            }

            m_DuplicatesCollection.Clear();
            foreach (FilesGroup group in newGroupCollection)
            {
                FilesGroup newGroup = new FilesGroup(group.Name);
                foreach (File file in group)
                    newGroup.Add(file);
                m_DuplicatesCollection.Add(newGroup);
            }
            m_DuplicatesCollection.Invalidate();
        }
    } // class DataModel

   
}
