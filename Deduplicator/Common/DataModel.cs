using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System.Threading;
using System.Linq;

namespace Deduplicator.Common
{

    public sealed class DataModel : INotifyPropertyChanged
    {
        public enum SearchStatus {
            Sorting,
            Grouping,
            GroupingCompleted,
            GroupingCanceled,
            SearchCompleted,
            SearchCanceled,
            NewFileSelected,
            ResultsCleared,
            JustInitialazed,
            ComparingStarted,
            Comparing,
            StartCancelOperation,
            Error
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
        //public Folder PrimaryFolder = null;

        #region Fields
        // Список каталогов в которых искать дубликаты
        private ObservableCollection<Folder> _foldersCollection = new ObservableCollection<Folder>();
        // Список найденых дубликатов файлов сгруппированных по заданному аттрибуту
        private GroupedFilesCollection _duplicatesCollection; // = new GroupedFilesCollection(progress);
        // Список файлов отобранных из каталогов в которых искать дубликаты    
        private FilesGroup _filesCollection;
        // Список аттрибутов по которым будет выполняться сравнение файлов при поиске дубликатов
        private FileCompareOptions _fileCompareOptions = new FileCompareOptions();
        private DateTime _startTime = DateTime.Now;

        private CancellationTokenSource _tokenSource;
        private Progress<OperationStatus> _progress;

        #endregion

        #region Properties

        public FileCompareOptions FileCompareOptions => _fileCompareOptions;

        public ObservableCollection<Folder> Folders => _foldersCollection;

        public GroupedFilesCollection DuplicatedFiles => _duplicatesCollection;

        private SearchStatus _status = SearchStatus.JustInitialazed;
        public SearchStatus Status => _status; 
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

        public int  FoldersCount => _foldersCollection.Count;
        public int  DuplicatesCount => _duplicatesCollection.Count; 
        public bool OperationCompleted => _status == SearchStatus.SearchCanceled ||
                                          _status == SearchStatus.SearchCompleted ||
                                          _status == SearchStatus.GroupingCompleted ||
                                          _status == SearchStatus.GroupingCanceled ||
                                          _status == SearchStatus.JustInitialazed ||
                                          _status == SearchStatus.JustInitialazed ||
                                          _status == SearchStatus.Error
                                          ? true : false;

        private Settings _settings = new Settings();
        public Settings Settings => _settings;
        // Первичный каталог (если определён)
        public Folder PrimaryFolder => Folders.FirstOrDefault(folder => folder.IsPrimary);

        #endregion

        public DataModel()
        {
            Settings.Restore();
            _progress = new Progress<OperationStatus>(ReportStatus);
            _duplicatesCollection = new GroupedFilesCollection(_progress);
            _filesCollection = new FilesGroup(_progress);
        }
 
        public async Task StartSearch(FileSelectionOptions selectionOptions, ObservableCollection<GroupingAttribute> compareAttribsList)
        {
            _tokenSource = new CancellationTokenSource();
            CancellationToken token = _tokenSource.Token;

            WorkItemHandler workhandler = delegate { Search(selectionOptions, compareAttribsList, token); };
            await ThreadPool.RunAsync(workhandler, WorkItemPriority.High, WorkItemOptions.TimeSliced);
        }

        /// <summary>
        /// Поиск дубликатов файлов
        /// </summary>
        /// <returns></returns>
        private async void Search( FileSelectionOptions selectionOptions, 
                                   ObservableCollection<GroupingAttribute> compareAttribsList, 
                                   CancellationToken cancelToken )
        {
            OperationStatus status = new OperationStatus { Id = SearchStatus.NewFileSelected };

            _filesCollection.Clear();
            _duplicatesCollection.Clear();
            try
            {
                // Отберём файлы из заданных пользователем каталогов для дальнейшего анализа в FilesCollection
                foreach (Folder folder in _foldersCollection)
                    await GetFolderFiles(folder, _filesCollection, selectionOptions, cancelToken, status);

                //// Если нашлись файлы подходящие под условия фильтра то выполняем среди них поиск дубликатов
                if (_filesCollection.Count > 1)
                {
                    var fg = new FilesGroup(_progress);
                    foreach (File file in _filesCollection)
                        fg.Add(file);

                    _duplicatesCollection.Add(fg);

                    await _duplicatesCollection.RemoveNonDuplicates(compareAttribsList, cancelToken);
                }
                
                if (PrimaryFolder != null)
                {  // Дополнительно удалим из списка дубликатов файлы не дублирующие файлы из PrimaryFolder
                    DeleteNonPrimaryFolderDuplicates();
                }
                else
                {  // Или перегруппируем файлы по атрибуту выбранному в ComboBox для группировки
                    await _duplicatesCollection.RegroupDuplicates(_fileCompareOptions.SelectedGroupAttrib, cancelToken);
                }

                status.Id = SearchStatus.SearchCompleted;
                ((IProgress<OperationStatus>)_progress).Report(status);
            }
            catch (OperationCanceledException)
            {
                _filesCollection.Clear();
                _duplicatesCollection.Clear();
                if (status.Id != SearchStatus.Error)
                    status.Id = SearchStatus.SearchCanceled;
                ((IProgress<OperationStatus>)_progress).Report(status);
            }
        }

 
        // Для каждого файла из Primary folder показать его дубликаты в других каталогах
        private void DeleteNonPrimaryFolderDuplicates()
        {
            var groupsForDelete = new List<FilesGroup>();
            foreach (FilesGroup group in _duplicatesCollection)
            {
                var fileFromPrimariFolder = group.FirstOrDefault(file => file.FromPrimaryFolder);

                if (fileFromPrimariFolder != null)
                {
                    group.Name = fileFromPrimariFolder.Name;
                    group.Remove(fileFromPrimariFolder);
                }
                else
                { // если нет то помещаем группу в список для последующего удаления
                    groupsForDelete.Add(group);
                }
            }
            // Удалим тз результатов поиска все группы не содержащие файлов из Primary folder
            foreach (FilesGroup group in groupsForDelete)
                _duplicatesCollection.Remove(group);
        }

  
        private void ReportStatus(OperationStatus status)
        {
            _status = status.Id;
            switch(status.Id)
            {
                case SearchStatus.NewFileSelected:
                    SearchStatusInfo = string.Format(@"Selecting files. Total files selected {0}.", status.HandledItems);
                    break;
                case SearchStatus.Grouping:
                    SearchStatusInfo = string.Format(@"Groupping files by {0}. Handled {1} files from {2}.",
                                                    status.Stage, status.HandledItems, status.TotalItems);
                    break;
                case SearchStatus.GroupingCompleted:
                    SearchStatusInfo = string.Format(@"Grouping complete. Regrouped {0} duplicates into {1} groups.",
                                                      _duplicatesCollection.FilesCount, _duplicatesCollection.Count);
                    break;

                case SearchStatus.ComparingStarted:
                case SearchStatus.Comparing:
                    SearchStatusInfo = string.Format(@"Comparing files by {0}. Compared {1} files from {2}.",
                                                   status.Stage, status.HandledItems, status.TotalItems);
                    break;
                case SearchStatus.Sorting:
                    SearchStatusInfo = @"Sorting files ...";
                    break;
                case SearchStatus.SearchCompleted:
                    SearchStatusInfo = string.Format(@"Search completed. Found {0} duplicates in {1} groups.",
                                                      _duplicatesCollection.FilesCount, _duplicatesCollection.Count);
                    break;
                case SearchStatus.SearchCanceled:
                    SearchStatusInfo = string.Format(@"Search canceled.");
                    break;
                case SearchStatus.GroupingCanceled:
                    SearchStatusInfo = string.Format(@"Grouping canceled.");
                    break;
                case SearchStatus.ResultsCleared:
                    SearchStatusInfo = string.Format(@"Search results cleared.");
                    break;
                case SearchStatus.StartCancelOperation:
                    SearchStatusInfo = string.Format(@"Canceling current operation.");
                    break;
                case SearchStatus.Error:
                    status.Message = status.Message.Replace('\r', ' ');
                    status.Message = status.Message.Replace('\n', ' ');
                    SearchStatusInfo = string.Format(@"Error: {0} Operation canceled.", status.Message);
                    break;
            }
            NotifySearchStatusChanged(status.Id);
            if (OperationCompleted)
                _duplicatesCollection.Invalidate();
        }

        public void CancelOperation()
        {
            OperationStatus status = new OperationStatus { Id = SearchStatus.StartCancelOperation };
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
            catch (FileNotFoundException ex)
            {
                status.Id = SearchStatus.Error;
                status.Message = ex.Message+"'" + folder.FullName+"'";                
                throw new OperationCanceledException();
            }

            var progress = _progress as IProgress<OperationStatus>;
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
                    catch(Exception ex)
                    {
                        status.Id = SearchStatus.Error;
                        status.Message = ex.Message + "'"+ item.Name + "'.";
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
            _duplicatesCollection.Invalidate();
        }

        private async void Regroup( GroupingAttribute attribute, CancellationToken token)
        {
            GroupedFilesCollection rollbackGroupsBuffer = new GroupedFilesCollection(_progress);
            // Сохраним результаты предыдущей сортировки для восстановления в случае отката операции
            foreach (var group in _duplicatesCollection)
                rollbackGroupsBuffer.Add(group);
            // Разделим полученный ранее полный список дубликатов на группы по указанному атрибуту
            try
            {
                await _duplicatesCollection.RegroupDuplicates(attribute, token);
            }
            catch (OperationCanceledException)
            {
                _duplicatesCollection.Clear();
                // Восстановим результаты предыдущей сортировки
                foreach (var group in rollbackGroupsBuffer)
                    _duplicatesCollection.Add(group);
                OperationStatus status = new OperationStatus { Id = SearchStatus.GroupingCanceled };
                ((IProgress<OperationStatus>)_progress).Report(status);
            }
        }

       public void ClearSearchResults()
        {
            _filesCollection.Clear();
            _duplicatesCollection.Clear();
            OperationStatus status = new OperationStatus  { Id = DataModel.SearchStatus.ResultsCleared };
            ReportStatus(status);
        }

        public void SetFilesProtection(Folder folder, bool isProtected)
        {
            GroupedFilesCollection newGroupCollection = new GroupedFilesCollection(_progress);
            foreach (var group in _duplicatesCollection)
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

            _duplicatesCollection.Clear();
            foreach (FilesGroup group in newGroupCollection)
            {
                FilesGroup newGroup = new FilesGroup(group.Name);
                foreach (File file in group)
                    newGroup.Add(file);
                _duplicatesCollection.Add(newGroup);
            }
            _duplicatesCollection.Invalidate();
        }
    } // class DataModel

   
}
