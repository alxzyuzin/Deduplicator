using System.Collections.Generic;
using System.ComponentModel;

namespace Deduplicator.Common
{
    public class FileCompareOptions: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyname)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyname));
        }

        private bool _checkName;
        public bool CheckName
        {
            get { return _checkName; }
            set
            {
                if(_checkName!=value)
                {
                    _checkName = value;
                    NotifyPropertyChanged("CheckName");
                }
            }
        }

        private bool _checkSize;
        public bool CheckSize
        {
            get { return _checkSize; }
            set
            {
                if (_checkSize!=value)
                {
                    _checkSize = value;
                    NotifyPropertyChanged("CheckSize");
                }
            }
        }

        private bool _checkCreationDateTime;
        public bool CheckCreationDateTime
        {
            get { return _checkCreationDateTime; }
            set
            {
                if(_checkCreationDateTime!=value)
                {
                    _checkCreationDateTime = value;
                    NotifyPropertyChanged("CheckCreationDateTime");
                }
            }
        }

        private bool _checkModificationDateTime;
        public bool CheckModificationDateTime
        {
            get { return _checkModificationDateTime; }
            set
            {
                if (_checkModificationDateTime!=value)
                {
                    _checkModificationDateTime = value;
                    NotifyPropertyChanged("CheckModificationDateTime");
                }
            }
        }

        private bool _checkContent;
        public bool CheckContent
        {
            get { return _checkContent; }
            set
            {
                if (_checkContent != value)
                {
                    _checkContent = value;
                    NotifyPropertyChanged("CheckContent");
                }
            }
        }

        public FileCompareOptions()
        {
            CheckName = true;
            CheckSize = true;
        }
        public List<FileAttribs> CheckAttribsList
        {
            get
            {
                List<FileAttribs> checkattribslist = new List<FileAttribs>();
                 
                if (CheckName)
                    checkattribslist.Add(FileAttribs.Name);
                if (CheckSize | CheckContent)
                    checkattribslist.Add(FileAttribs.Size);
                if (CheckCreationDateTime)
                    checkattribslist.Add(FileAttribs.DateCreated);
                if (CheckModificationDateTime)
                    checkattribslist.Add(FileAttribs.DateModified);
                if (CheckContent)
                    checkattribslist.Add(FileAttribs.Content);

                return checkattribslist;
            }
        }
    }
}
