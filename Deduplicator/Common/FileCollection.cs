using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Windows.UI.Core;

namespace Deduplicator.Common
{
    public sealed class FileCollection : ObservableCollection<File>
    {
        public override event NotifyCollectionChangedEventHandler CollectionChanged;

        private delegate void NotifyCollectionItemPropertyChangedEventHandler();

        private async void NotifyCollectionChanged()
        {
            if (CollectionChanged != null)
            {
                await _mainPage.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, delegate { CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)); });
            }
        }

        private MainPage _mainPage;

        public MainPage MainPage { get { return _mainPage; } set { _mainPage = value; } }

        public FileCollection()
        {

        }

        public FileCollection(MainPage mainpage)
        {
            _mainPage = mainpage;
        }

        public new void Add(File file)
        {
            base.Add(file);
            //NotifyCollectionChanged();
        }

        public new void Clear()
        {
            base.Clear();
            NotifyCollectionChanged();
        }

        public void Invalidate()
        {
            NotifyCollectionChanged();

        }
        
        public int Sort()
        {
            int PassCount = 0;
            bool CollectionSorted = true;
            do
            {
                PassCount++;
                CollectionSorted = true;
                for (int i = 0; i < (this.Count - 1); i++)
                {
                    if (this[i].Size > this[i + 1].Size)
                    {
                        MoveItem(i, i + 1);
                        CollectionSorted = false;
                    }
                }
            } while (!CollectionSorted);

            NotifyCollectionChanged();
            return PassCount;
        }

        /// <summary>
        // Удаляет из FilesCollection файлы, явно не являющиеся дублями.
        // Если для выбранного файла в коллекции есть файл такого же размера то эти файлы
        // могут быть дублями. Такие файлы оставляем для дальнейшего анализа
        /// </summary>
        public void DeleteNonDuplicates()
        {
            if (this.Count == 0)
                return;
            // Пометим дубли в списке файлов
            if (this[0].Size == this[1].Size)
                this[0].Duplicated = true;
            for (int i = 1; i < this.Count - 1; i++)
                if (this[i - 1].Size == this[i].Size || this[i].Size == this[i + 1].Size)
                    this[i].Duplicated = true;
            if (this[this.Count - 2].Size == this[this.Count - 1].Size)
                this[this.Count - 1].Duplicated = true;

            // Перенесём дубликаты во временный список
            FileCollection TempFileCollection = new FileCollection();

            foreach (File file in this)
            {
                if (file.Duplicated)
                    TempFileCollection.Add(file);
            }

            //Очистим исходный список файлов
            this.Clear();
            // Вернём дубликаты назад
            foreach (File file in TempFileCollection)
            {
                this.Add(file);
            }
            NotifyCollectionChanged();
        }
        

        

    }
}

