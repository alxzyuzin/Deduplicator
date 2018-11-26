using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace Deduplicator.Common
{
    public class GroupedFilesCollection : ObservableCollection<FilesGroup>
    {
        public override event NotifyCollectionChangedEventHandler CollectionChanged;

        private void NotifyCollectionChanged()
        {
            if (CollectionChanged != null)
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        GroupingAttribute m_lastAttributeUsedForGrouping = null;

        public int FilesCount
        {
            get
            {
                int i = 0;
                foreach (FilesGroup group in this)
                    i += group.Count;
                return i;
            }
        }

        private Progress<OperationStatus> m_progress = null;

        public GroupedFilesCollection(Progress<OperationStatus> progress)
        {
            m_progress = progress;
        }
  
        public void Invalidate()
        {
            NotifyCollectionChanged();
        }


        public async Task RemoveNonDuplicates(ObservableCollection<GroupingAttribute> attributeList,
                                                CancellationToken cancelToken)
        {
            IProgress<OperationStatus> progress = m_progress;
            var status = new OperationStatus
            {
                Id = DataModel.SearchStatus.Comparing,
                TotalItems = this[0].Count,
                HandledItems = 0,
                Stage = string.Empty
            } ;
            
            var groupsBuffer = new GroupedFilesCollection(m_progress);

            IEnumerable<GroupingAttribute> query = from attribute in attributeList
                                                   orderby attribute.Weight ascending
                                                   select attribute;
            foreach (var attribute in query)
            {
                if (attribute.Attribute == FileAttribs.None)
                    continue;
                status.HandledItems = 0;
                status.Stage = attribute.Name;
                progress.Report(status);

                groupsBuffer.Clear();
                foreach (FilesGroup group in this)
                    groupsBuffer.Add(group);

                this.Clear();
                foreach (FilesGroup group in groupsBuffer)
                {
                    List<FilesGroup> splitResult = await group.SplitByAttribute(attribute, cancelToken, status);
                    
                    foreach (FilesGroup newGroup in splitResult)
                        this.Add(newGroup);
                 }
                m_lastAttributeUsedForGrouping = attribute;
             }
        }

        public async Task RegroupDuplicates(GroupingAttribute attribute, CancellationToken cancelToken)
        {
            // Соберём все файлы в одну группу
            FilesGroup allFiles = new FilesGroup(m_progress);
            foreach (FilesGroup group in this)
                foreach (File file in group)
                    allFiles.Add(file);

            IProgress<OperationStatus> progress = m_progress;
            OperationStatus status = new OperationStatus
            {
               Id = DataModel.SearchStatus.Grouping,
               HandledItems = 0,
               TotalItems = allFiles.Count,
               Stage = attribute.Name
             };

            this.Clear();

            if (attribute.Attribute == FileAttribs.None)
            {
                this.Add(allFiles);
            }
            else
            {
                List<FilesGroup> splitResult = await allFiles.SplitByAttribute(attribute, cancelToken, status);
                for (int i = 0; i < splitResult.Count; i++)
                    this.Add(splitResult[i]);
            }
            m_lastAttributeUsedForGrouping = attribute;
            status.Id = DataModel.SearchStatus.GroupingCompleted;
            progress.Report(status);
        }

    } // Class  GroupedFilesCollection


    /// <summary>
    /// Представляет собой группу файлов
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FilesGroup : ObservableCollection<File>
    {
        public override event NotifyCollectionChangedEventHandler CollectionChanged;
        private void NotifyCollectionChanged()
        {
            if (CollectionChanged != null)
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public string Name { get; set; } = string.Empty;
        public ulong TotalSize { get; set; } = 0;
        public ulong FileSize { get; set; } = 0;
        private Progress<OperationStatus> m_progress = null;

        public new IEnumerator<object> GetEnumerator()
        {
            return (IEnumerator<object>)base.GetEnumerator();
        }
        
        public FilesGroup(Progress<OperationStatus> progress)
        {
            m_progress = progress;
        }

        public FilesGroup( string name)
        {
            Name = name;
        }

        /// <summary>
        /// Реализует алгоритм сортировки спика файлов по заданному аттрибуту
        /// </summary>
        /// <param name="files"></param>
        /// <param name="firstindex"></param>
        /// <param name="lastindex"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public async Task SortByAttribute(int firstindex, int lastindex, FileAttribs option)
        {
            if (firstindex >= lastindex)
                return;

            int c = await SortPass(firstindex, lastindex, option);
            await SortByAttribute(firstindex, c - 1, option);
            await SortByAttribute(c + 1, lastindex, option);
        }

        /// <summary>
        /// Одиночный проход сортировки
        /// </summary>
        /// <param name="files"></param>
        /// <param name="firstindex"></param>
        /// <param name="lastindex"></param>
        /// <param name="compareattrib"></param>
        /// <returns></returns>
        private async Task<int> SortPass( int firstindex, int lastindex, FileAttribs compareattrib)
        {
            int i = firstindex;

            for (int j = i; j <= lastindex; j++)
            {
                int compareResult = await this[j].CompareTo(this[lastindex], compareattrib);
                if (compareResult <= 0)
                {
                    File t = this[i];
                    this[i] = this[j];
                    this[j] = t;
                    ++i;
                }
            }
            return i - 1;
        }

        /// <summary>
        /// Split group to smoll groups with equal value of given attribute
        /// Выполняем сортировку группы по заданному атрибуту файла
        /// Сравниваем файлы в группе по заданному атрибуту попарно первый со вторым второй с третьим и тд
        /// и при равенстве знвчений заданного атрибута добавляем файлы в новую группу
        /// при первом несовпадении файлов считаем формирование группы файлов совпадающих по заданному атрибуту
        /// завершенным.
        /// Если количество файлов в новой группе больше 1, новую группу добавляем в новый список групп 
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="status"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        public async Task<List<FilesGroup>> SplitByAttribute(GroupingAttribute attribute,
                                            CancellationToken cancelToken, OperationStatus status)
        {
            Debug.Assert(this.Count >= 2, "SplitByAttributeMethod called on group with less then two files");

            IProgress<OperationStatus> progress = m_progress;

            DataModel.SearchStatus oldStatusId = status.Id;

            status.Id = DataModel.SearchStatus.Sorting;
            progress.Report(status);
            await this.SortByAttribute(0, this.Count - 1, attribute.Attribute);

            status.Id = oldStatusId;
            var newGroupsCollection = new List<FilesGroup>();

            var newgroup = new FilesGroup(m_progress);
            newgroup.Add(this[0]);
            ++status.HandledItems;
            for (int i = 1; i < this.Count; i++)
            {
               int compareResult = await this[i - 1].CompareTo(this[i], attribute.Attribute);
               if (compareResult != 0)
               {
                    if (newgroup.Count > 1)
                        newGroupsCollection.Add(newgroup);
                        newgroup = new FilesGroup(m_progress);
               }
               newgroup.Add(this[i]);

               cancelToken.ThrowIfCancellationRequested();
               ++status.HandledItems;
               progress.Report(status);
            }

            if (newgroup.Count > 1)
                newGroupsCollection.Add(newgroup);
            return newGroupsCollection;
        }

        public void Invalidate()
        {
            NotifyCollectionChanged();
        }

    }  // Class FilesGroup

    public class OperationStatus
    {
        public DataModel.SearchStatus Id = DataModel.SearchStatus.JustInitialazed;
        public string Stage = string.Empty;
        public int TotalItems = 0;
        public int HandledItems = 0;
        public string Message = string.Empty;
    } // class OperationStatus
}
