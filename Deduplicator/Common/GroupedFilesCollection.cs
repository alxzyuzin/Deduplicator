using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Core;

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

        public GroupingAttribute CurrentGroupingAttribute { get; private set; }
        private Progress<OperationStatus> m_progress = null;

        public GroupedFilesCollection()
        {

        }

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
            OperationStatus status = new OperationStatus
            {
                Id = DataModel.SearchStatus.Analyse,
                TotalItems = this[0].Count,
                HandledItems = 0,
                Stage = string.Empty
            } ;
            
            GroupedFilesCollection groupsBuffer = new GroupedFilesCollection();

            foreach (var attribute in attributeList)
            {
                status.Stage = string.Format(@"Select duplicates using {0}.", attribute.Name);
                status.Id = DataModel.SearchStatus.Comparing;
                
                CurrentGroupingAttribute = attribute;
                if (attribute.Attribute == FileAttribs.None)
                    continue;
                ((IProgress<OperationStatus>)m_progress).Report(status);

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
 
            }
         }

        public async Task RegroupDuplicates(GroupingAttribute attribute, CancellationToken cancelToken)
        {
            OperationStatus status = new OperationStatus
            {
                Id = DataModel.SearchStatus.Grouping,
                TotalItems = this[0].Count,
                HandledItems = 0,
                Stage = string.Format(@"Regrouping duplicates using {0}.", attribute.Name)
            }; 

            if (CurrentGroupingAttribute.Attribute == attribute.Attribute)
                return;
            CurrentGroupingAttribute = attribute;
            // Соберём все файлы в одну группу
            FilesGroup allFiles = new FilesGroup();
            foreach (FilesGroup group in this)
                foreach(File file in group)
                    allFiles.Add(file);

            this.Clear();

            if (attribute.Attribute == FileAttribs.None)
            {
                this.Add(allFiles);
            }
            else
            {
                List<FilesGroup> splitResult = await allFiles.SplitByAttribute(attribute, cancelToken, status);
                for ( int i = 0; i<splitResult.Count;i++)
                    this.Add(splitResult[i]);
            }
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

        public string Name { get; set; }
        public ulong  TotalSize { get; set; }
        public ulong  FileSize { get; set; }
        private Progress<OperationStatus> m_progress = null;

        public new IEnumerator<object> GetEnumerator()
        {
            return (IEnumerator<object>)base.GetEnumerator();
        }
        
        public FilesGroup()
        { }
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
                                            CancellationToken cancelToken, OperationStatus operationStatus)
        {
            Debug.Assert(this.Count >=2 ,"SplitByAttributeMethod called on group with less then two files");
#if DEBUG
            if (this.Count < 2)
            {
                int i = 0;
            }
#endif
            await this.SortByAttribute(0, this.Count - 1, attribute.Attribute);

            List<FilesGroup> newGroupsCollection = new List<FilesGroup>();
            FilesGroup newgroup = new FilesGroup(m_progress);

            IProgress<OperationStatus> progress = m_progress;
            int compareResult = -1;
            newgroup.Add(this[0]);
            for (int i = 1; i < this.Count; i++)
            {
                cancelToken.ThrowIfCancellationRequested();
                ++operationStatus.HandledItems;

#if DEBUG
                if (progress == null)
                {
                    int x = 0;
                }
#endif
                //progress.Report(DataModel.SearchStatus.Analyse);
                progress.Report(operationStatus);
                compareResult = await this[i-1].CompareTo(this[i], attribute.Attribute);
                if (compareResult != 0)
                {
                    if (newgroup.Count > 1)
                        newGroupsCollection.Add(newgroup);
                    newgroup = new FilesGroup(m_progress);
                }
                newgroup.Add(this[i]);
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
        public DataModel.SearchStatus Id = DataModel.SearchStatus.UnDefined;
        public string Stage = string.Empty;
        public int TotalItems = 0;
        public int HandledItems = 0;


    } // class OperationStatus
}
