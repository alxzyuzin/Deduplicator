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
        private ObservableCollection<Folder> m_foldersCollection = new ObservableCollection<Folder>();
        // Список найденых дубликатов файлов сгруппированных по заданному аттрибуту
        private GroupedFilesCollection m_duplicatesCollection; // = new GroupedFilesCollection(progress);
        // Список файлов отобранных из каталогов в которых искать дубликаты    
        private FilesGroup m_filesCollection;
        // Список аттрибутов по которым будет выполняться сравнение файлов при поиске дубликатов
        private FileCompareOptions _fileCompareOptions = new FileCompareOptions();
        private DateTime m_startTime = DateTime.Now;

        private CancellationTokenSource m_tokenSource;
        private Progress<OperationStatus> m_progress;

        #endregion

        #region Properties

        public FileCompareOptions FileCompareOptions
        {
            get { return _fileCompareOptions; }
        }

        public ObservableCollection<Folder> Folders
        {
            get {return m_foldersCollection;}
        }

        public GroupedFilesCollection DuplicatedFiles
        {
            get { return m_duplicatesCollection; }
        }

        private SearchStatus m_status = SearchStatus.JustInitialazed;
        public SearchStatus Status { get { return m_status; } }

        private string m_searchStatusInfo = string.Empty;
        public string SearchStatusInfo
        { get
            { return m_searchStatusInfo; }
            set
            {
                if (m_searchStatusInfo != value)
                {
                    m_searchStatusInfo = value;
                    NotifyPropertyChanged("SearchStatusInfo");
                }
            }
        }

        public int  FoldersCount
        {
            get { return m_foldersCollection.Count; }
        }

        public int  DuplicatesCount { get { return m_duplicatesCollection.Count; } }

        public bool PrimaryFolderSelected { get; set; }

        public bool OperationCompleted
        {
            get
            {
                return
                       m_status == SearchStatus.SearchCanceled ||
                       m_status == SearchStatus.SearchCompleted ||
                       m_status == SearchStatus.GroupingCompleted ||
                       m_status == SearchStatus.GroupingCanceled ||
                       m_status == SearchStatus.JustInitialazed ||
                       m_status == SearchStatus.JustInitialazed ||
                       m_status == SearchStatus.Error
                       ? true : false;
            }
        }

        private Settings _settings = new Settings();
        public Settings Settings
        {
            get { return _settings; }
        }
        // Первичный каталог (если определён)
        public Folder PrimaryFolder
        {
            get
            {
               return Folders.FirstOrDefault(folder => folder.IsPrimary);
            }
        } 
        #endregion

        public DataModel()
        {
            Settings.Restore();
            m_progress = new Progress<OperationStatus>(ReportStatus);
            m_duplicatesCollection = new GroupedFilesCollection(m_progress);
            m_filesCollection = new FilesGroup(m_progress);
        }
 
        public async Task StartSearch(FileSelectionOptions selectionOptions, ObservableCollection<GroupingAttribute> compareAttribsList)
        {
            m_tokenSource = new CancellationTokenSource();
            CancellationToken token = m_tokenSource.Token;

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

            m_filesCollection.Clear();
            m_duplicatesCollection.Clear();
            try
            {
                // Отберём файлы из заданных пользователем каталогов для дальнейшего анализа в FilesCollection
                foreach (Folder folder in m_foldersCollection)
                    await GetFolderFiles(folder, m_filesCollection, selectionOptions, cancelToken, status);

                //// Если нашлись файлы подходящие под условия фильтра то выполняем среди них поиск дубликатов
                if (m_filesCollection.Count > 1)
                {
                    var fg = new FilesGroup(m_progress);
                    foreach (File file in m_filesCollection)
                        fg.Add(file);

                    m_duplicatesCollection.Add(fg);

                    await m_duplicatesCollection.RemoveNonDuplicates(compareAttribsList, cancelToken);
                }
                
                if (PrimaryFolder != null)
                {  // Дополнительно удалим из списка дубликатов файлы не дублирующие файлы из PrimaryFolder
                    DeleteNonPrimaryFolderDuplicates();
                }
                else
                {  // Или перегруппируем файлы по атрибуту выбранному в ComboBox для группировки
                    await m_duplicatesCollection.RegroupDuplicates(_fileCompareOptions.SelectedGroupAttrib, cancelToken);
                }

                status.Id = SearchStatus.SearchCompleted;
                ((IProgress<OperationStatus>)m_progress).Report(status);
            }
            catch (OperationCanceledException)
            {
                m_filesCollection.Clear();
                m_duplicatesCollection.Clear();
                if (status.Id != SearchStatus.Error)
                    status.Id = SearchStatus.SearchCanceled;
                ((IProgress<OperationStatus>)m_progress).Report(status);
            }
        }

 
        // Для каждого файла из Primary folder показать его дубликаты в других каталогах
        private void DeleteNonPrimaryFolderDuplicates()
        {
            var groupsForDelete = new List<FilesGroup>();
            foreach (FilesGroup group in m_duplicatesCollection)
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
                m_duplicatesCollection.Remove(group);
        }

  
        private void ReportStatus(OperationStatus status)
        {
            m_status = status.Id;
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
                                                      m_duplicatesCollection.FilesCount, m_duplicatesCollection.Count);
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
                                                      m_duplicatesCollection.FilesCount, m_duplicatesCollection.Count);
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
                m_duplicatesCollection.Invalidate();
        }

        public void CancelOperation()
        {
            OperationStatus status = new OperationStatus { Id = SearchStatus.StartCancelOperation };
            ReportStatus(status);
            m_tokenSource.Cancel();
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

            var progress = m_progress as IProgress<OperationStatus>;
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
            m_tokenSource = new CancellationTokenSource();
            CancellationToken token = m_tokenSource.Token;

            WorkItemHandler workhandler = delegate { Regroup(attribute, token); };
            await ThreadPool.RunAsync(workhandler, WorkItemPriority.High, WorkItemOptions.TimeSliced);
            m_duplicatesCollection.Invalidate();
        }

        private async void Regroup( GroupingAttribute attribute, CancellationToken token)
        {
            GroupedFilesCollection rollbackGroupsBuffer = new GroupedFilesCollection(m_progress);
            // Сохраним результаты предыдущей сортировки для восстановления в случае отката операции
            foreach (var group in m_duplicatesCollection)
                rollbackGroupsBuffer.Add(group);
            // Разделим полученный ранее полный список дубликатов на группы по указанному атрибуту
            try
            {
                await m_duplicatesCollection.RegroupDuplicates(attribute, token);
            }
            catch (OperationCanceledException)
            {
                m_duplicatesCollection.Clear();
                // Восстановим результаты предыдущей сортировки
                foreach (var group in rollbackGroupsBuffer)
                    m_duplicatesCollection.Add(group);
                OperationStatus status = new OperationStatus { Id = SearchStatus.GroupingCanceled };
                ((IProgress<OperationStatus>)m_progress).Report(status);
            }
        }

       public void ClearSearchResults()
        {
            m_filesCollection.Clear();
            m_duplicatesCollection.Clear();
            OperationStatus status = new OperationStatus  { Id = DataModel.SearchStatus.ResultsCleared };
            ReportStatus(status);
        }

        public void SetFilesProtection(Folder folder, bool isProtected)
        {
            GroupedFilesCollection newGroupCollection = new GroupedFilesCollection(m_progress);
            foreach (var group in m_duplicatesCollection)
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

            m_duplicatesCollection.Clear();
            foreach (FilesGroup group in newGroupCollection)
            {
                FilesGroup newGroup = new FilesGroup(group.Name);
                foreach (File file in group)
                    newGroup.Add(file);
                m_duplicatesCollection.Add(newGroup);
            }
            m_duplicatesCollection.Invalidate();
        }
    } // class DataModel

   
}
