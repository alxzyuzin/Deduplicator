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
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public GroupedFilesCollection()
        {

        }

  
        public void Invalidate()
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
