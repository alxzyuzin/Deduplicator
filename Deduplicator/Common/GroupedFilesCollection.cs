﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.ComponentModel;
using System.Collections;

namespace Deduplicator.Common
{
    public class GroupedFilesCollection : ObservableCollection<FilesGroup>
    {
        public override event NotifyCollectionChangedEventHandler CollectionChanged;

        GroupingAttribute m_lastAttributeUsedForGrouping = null;

        public int FilesCount
        {
            get
            {
                return this.Aggregate(0, (total, next) => total += next.Count);
                //int i = 0;
                //foreach (FilesGroup group in this)
                //    i += group.Count;
                //return i;
            }
        }

        private Progress<OperationStatus> m_progress = null;

        public GroupedFilesCollection(Progress<OperationStatus> progress)
        {
            m_progress = progress;
        }
  
        public void RemoveGroup(FilesGroup group)
        {
            this.Remove(group);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
        public void Invalidate()
        {
             CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
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

            var localAttributeList = new List<GroupingAttribute>(attributeList);
            GroupingAttribute size = localAttributeList.FirstOrDefault<GroupingAttribute>(a => a.Attribute == FileAttribs.Size);
            GroupingAttribute content = localAttributeList.FirstOrDefault<GroupingAttribute>(a => a.Attribute == FileAttribs.Content);
            if (content != null && size == null)
                localAttributeList.Add(new GroupingAttribute("Size", FileAttribs.Size, 0));


            var groupsBuffer = new GroupedFilesCollection(m_progress);

            IEnumerable<GroupingAttribute> query = from attribute in localAttributeList
                                                   orderby attribute.Attribute ascending
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
            if (attribute == this.m_lastAttributeUsedForGrouping)
                return;
            
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
    public class FilesGroup : IEnumerable<File>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public IEnumerator<File> GetEnumerator() => (IEnumerator<File>)_files.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<File>)_files).GetEnumerator();

        private List<File> _files = new List<File>();
        private string _name;
        public string Name
        {
            get {return _name; }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
                }
            }
        }
        private ulong _totalSize = 0;
        public ulong  TotalSize
        {
            get
            {
                return _totalSize;
            }
            set
            {
                if (_totalSize != value)
                {
                    _totalSize = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalSize)));
                }
            }
        }
        private ulong _fileSize = 0;
        public ulong  FileSize
        {
            get
            {
                return _fileSize;
            }
        set
            {
                if (_fileSize != value)
                {
                    _fileSize = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FileSize)));
                }
            }

        }

        private bool? _isChecked=false; 
        public bool?  IsChecked
        {
            get
            { return _isChecked; }
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
                }
            }

        }
        public int Count  => _files.Count;

        public File FileFromPrimariFolder => _files.FirstOrDefault(file => file.FromPrimaryFolder);

        private Progress<OperationStatus> _progress = null;

        public FilesGroup(Progress<OperationStatus> progress)
        {
            _progress = progress;
        }

        public FilesGroup(FilesGroup group, Progress<OperationStatus> progress)
        {
            //foreach (var file in group)
            //    _files.Add(file);
            _files.AddRange(group);
            _progress = progress;
        }
        public FilesGroup( string name)
        {
            Name = name;
        }

        
        private void OnFilePropertyChanged(object obj, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsChecked")
            {
                IsChecked = null;
                int checkedCount = _files.Count(n => n.IsChecked);
                if (checkedCount == 0)
                    IsChecked = false;
                if (checkedCount == _files.Count)
                    IsChecked = true;
             }
        }

        public void Add(File file)
        {
            file.PropertyChanged += OnFilePropertyChanged; 
            _files.Add(file);
        }

        public bool Contains(File file)
        {
            return _files.Contains(file);
        }

        public void Remove(File file)
        {
            file.PropertyChanged -= OnFilePropertyChanged;
            _files.Remove(file);
        }

        public void Clear()
        {
            _files.Clear();
        }

        public FilesGroup Clone()
        {
            var newGroup = new FilesGroup(_progress);
            foreach (File file in _files)
                newGroup.Add(file);
            
            return newGroup;
        }

        public void AddAllGroupFilesToSelectedItems(IList<object> selectedItems)
        {
            if (!selectedItems.IsReadOnly)
            {
                foreach (File file in _files.Where(f=>!f.IsChecked))
                {
                    file.IsChecked = true;
                    selectedItems.Add(file);
                }
            }
        }

        public void RemoveAllGroupFilesFromSelectedItems(IList<object> selectedItems)
        {
            if (!selectedItems.IsReadOnly)
            {
                foreach (File file in _files.Where(f => f.IsChecked))
                {
                    file.IsChecked = false;
                    selectedItems.Remove(file);
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
                int compareResult = await _files[j].CompareTo(_files[lastindex], compareattrib);
                if (compareResult <= 0)
                {
                    File t = _files[i];
                    _files[i] = _files[j];
                    _files[j] = t;
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

            IProgress<OperationStatus> progress = _progress;

            DataModel.SearchStatus oldStatusId = status.Id;

            status.Id = DataModel.SearchStatus.Sorting;
            progress.Report(status);
            await this.SortByAttribute(0, this.Count - 1, attribute.Attribute);

            status.Id = oldStatusId;
            var newGroupsCollection = new List<FilesGroup>();

            var newgroup = new FilesGroup(_progress);
            newgroup.Add(_files[0]);
            ++status.HandledItems;
            for (int i = 1; i < this.Count; i++)
            {
               int compareResult = await _files[i - 1].CompareTo(_files[i], attribute.Attribute);
               if (compareResult != 0)
               {
                    if (newgroup.Count > 1)
                        newGroupsCollection.Add(newgroup);
                    newgroup = new FilesGroup(_progress);
               }
               newgroup.Add(_files[i]);

               cancelToken.ThrowIfCancellationRequested();
               ++status.HandledItems;
               progress.Report(status);
            }

            if (newgroup.Count > 1)
                newGroupsCollection.Add(newgroup);
            return newGroupsCollection;
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
