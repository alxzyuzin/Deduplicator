using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Windows.UI.Core;

namespace Deduplicator.Common
{
    public sealed class GroupedFilesCollection: ObservableCollection<FilesGroup>
    {
        public override event NotifyCollectionChangedEventHandler CollectionChanged;

        private delegate void NotifyCollectionItemPropertyChangedEventHandler();

        private void NotifyCollectionChanged()
        {
            if (CollectionChanged != null)
            {
                //   await _mainPage.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, delegate { CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)); });
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        private MainPage _mainPage = null;

        public MainPage MainPage { get { return _mainPage; } set { _mainPage = value; } }

        public GroupedFilesCollection()
        {

        }

        public GroupedFilesCollection(MainPage mainpage)
        {
            _mainPage = mainpage;
        }

        //        public new void Add(FilesGroup group)
        //        {
        //            base.Add(group);
        //        //    NotifyCollectionChanged();
        //        }

        //public new void Clear()
        //{
        //    base.Clear();
        //    //            await Task.Delay(TimeSpan.FromMilliseconds(100));
        //    NotifyCollectionChanged();
        //}

        public  void Invalidate()
        {
            NotifyCollectionChanged();
        }
    }



    /// <summary>
    /// Представляет собой группу файлов
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FilesGroup : ObservableCollection<File>
    {

        public string Name { get; set; }
        public ulong  TotalSize { get; set; }
        public ulong  FileSize { get; set; }
        
        public new IEnumerator<object> GetEnumerator()
        {
            return (IEnumerator<object>)base.GetEnumerator();
        }
        
        public FilesGroup()
        { }

        public FilesGroup( string name)
        {
            Name = name;
        }
    }
}
