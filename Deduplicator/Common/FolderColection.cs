using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Deduplicator.Common
{
    public sealed class FolderColection: ObservableCollection<Folder>, INotifyCollectionChanged
    {
        public override event NotifyCollectionChangedEventHandler CollectionChanged;

        private void NotifyCollectionChanged()
        {
            if (CollectionChanged != null)
            {
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        public new void Add(Folder folder)
        {
            base.Add(folder);
            NotifyCollectionChanged();
        }

        public new void Remove(Folder folder)
        {
            base.Remove(folder);
            NotifyCollectionChanged();
        }
    }
}
