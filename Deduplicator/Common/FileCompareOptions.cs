using System.Collections.Generic;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Deduplicator.Common
{
    public class FileCompareOptions : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyname)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyname));
        }

        private bool _isRollBack = false;
        public bool IsRollBack
        { get { return _isRollBack; } }

        private bool _checkNameOldValue;
        private bool _checkName;
        public bool CheckName
        {
            get { return _checkName; }
            set
            {
                if (_checkName != value)
                {
                    _checkName = value;
                    NotifyPropertyChanged("CheckName");
//                    UpdateActiveAttribsLists(FileAttribs.Name, value);
                }
            }
        }

        private bool _checkSizeOldValue;
        private bool _checkSize;
        public bool CheckSize
        {
            get { return _checkSize; }
            set
            {
                if (_checkSize != value)
                {
                    _checkSize = value;
                    NotifyPropertyChanged("CheckSize");
//                    UpdateActiveAttribsLists(FileAttribs.Size, value);
                }
            }
        }

        private bool _checkCreationDateTimeOldValue;
        private bool _checkCreationDateTime;
        public bool CheckCreationDateTime
        {
            get { return _checkCreationDateTime; }
            set
            {
                if (_checkCreationDateTime != value)
                {
                    _checkCreationDateTime = value;
                    NotifyPropertyChanged("CheckCreationDateTime");
//                    UpdateActiveAttribsLists(FileAttribs.DateCreated, value);
                }
            }
        }

        private bool _checkModificationDateTimeOldValue;
        private bool _checkModificationDateTime;
        public bool CheckModificationDateTime
        {
            get { return _checkModificationDateTime; }
            set
            {
                if (_checkModificationDateTime != value)
                {
                    _checkModificationDateTime = value;
                    NotifyPropertyChanged("CheckModificationDateTime");
//                    UpdateActiveAttribsLists(FileAttribs.DateModified, value);
                }
            }
        }

        private bool _checkContentOldValue;
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
//                    UpdateActiveAttribsLists(FileAttribs.Content, value);
                }
            }
        }

        public ObservableCollection<string> _resultGrouppingModes = new ObservableCollection<string>();
        public ObservableCollection<string> ResultGrouppingModesList { get { return _resultGrouppingModes; } }

        private List<FileAttribs> _compareAttribs = new List<FileAttribs>();
        public List<FileAttribs> CompareAttribsList { get { return _compareAttribs; } }

        public FileAttribs CurrentGroupModeAttrib
        {
            get
            {
                 return (_currentGroupModeIndex>=0)&&(_currentGroupModeIndex < CompareAttribsList.Count)?CompareAttribsList[_currentGroupModeIndex]: FileAttribs.None;
            }
        }

        private int _currentGroupModeIndexOldValue =0;
        private int _currentGroupModeIndex = 0;
        public  int CurrentGroupModeIndex
        {
            get { return _currentGroupModeIndex; }
            set
            {
                if (_currentGroupModeIndex != value)
                {
                    _currentGroupModeIndex = value;
                    NotifyPropertyChanged("CurrentGroupModeIndex");
                }
            }
        }

        public FileCompareOptions()
        {

            CheckName = true;
            CheckSize = true;
            UpdateActiveAttribsLists();
            CurrentGroupModeIndex = 0; 
            _checkNameOldValue = _checkName;
            _checkSizeOldValue = _checkSize;
            _checkCreationDateTimeOldValue = _checkCreationDateTime;
            _checkModificationDateTimeOldValue = _checkModificationDateTime;
            _checkContentOldValue = _checkContent;
            _currentGroupModeIndexOldValue = _currentGroupModeIndex;
        }

        private void UpdateActiveAttribsLists(FileAttribs attrib, bool operation)
        {
            if (operation)
                _compareAttribs.Add(attrib);
            else
                _compareAttribs.Remove(attrib);
            
            _resultGrouppingModes.Clear();

            foreach(var attr in _compareAttribs)
            {
                switch(attr)
                {
                    case FileAttribs.Content:
                        _resultGrouppingModes.Add("Content");
                        break;
                    case FileAttribs.Name:
                        _resultGrouppingModes.Add("Name");
                        break;
                    case FileAttribs.Size:
                        _resultGrouppingModes.Add("Size");
                        break;
                    case FileAttribs.DateCreated:
                        _resultGrouppingModes.Add("Creation date");
                        break;
                    case FileAttribs.DateModified:
                        _resultGrouppingModes.Add("Modification date");
                        break;

                }
            }
            _resultGrouppingModes.Add("Do not group files.");
           if (_resultGrouppingModes.Count == 1)
                CurrentGroupModeIndex = 0;
            NotifyPropertyChanged("ResultGrouppingModesList");
        }

        private void UpdateActiveAttribsLists()
        {
            _resultGrouppingModes.Clear();
            _compareAttribs.Clear();

            if (_checkName)
            {
                _resultGrouppingModes.Add("Name");
                _compareAttribs.Add(FileAttribs.Name);
            }
            if (_checkSize)
            {
                _resultGrouppingModes.Add("Size");
                _compareAttribs.Add(FileAttribs.Size);
            }
            if (_checkCreationDateTime)
            {
                _resultGrouppingModes.Add("Creation date");
                _compareAttribs.Add(FileAttribs.DateCreated);
            }
            if (_checkModificationDateTime)
            {
                _resultGrouppingModes.Add("Modification date");
                _compareAttribs.Add(FileAttribs.DateCreated);
            }
            if (_checkContent)
            {
                _resultGrouppingModes.Add("Content");
                _compareAttribs.Add(FileAttribs.Content);
            }

            _resultGrouppingModes.Add("Do not group files.");

            NotifyPropertyChanged("ResultGrouppingModesList");
//            CurrentGroupModeIndex = 0;
        }

        public void Commit()
        {
            _checkNameOldValue = _checkName;
            _checkSizeOldValue = _checkSize;
            _checkCreationDateTimeOldValue = _checkCreationDateTime;
            _checkModificationDateTimeOldValue = _checkModificationDateTime;
            _checkContentOldValue = _checkContent;
            UpdateActiveAttribsLists();
            //_currentGroupModeIndexOldValue = _currentGroupModeIndex;
        }

        public void RollBack()
        {
            _isRollBack = true;
            if (_checkName != _checkNameOldValue)
                CheckName = _checkNameOldValue;
            if (_checkSize != _checkSizeOldValue)
                CheckSize = _checkSizeOldValue;
            if (_checkCreationDateTime != _checkCreationDateTimeOldValue)
                CheckCreationDateTime = _checkCreationDateTimeOldValue;
            if (_checkModificationDateTime != _checkModificationDateTimeOldValue)
                CheckModificationDateTime = _checkModificationDateTimeOldValue;
            if (_checkContent != _checkContentOldValue)
                CheckContent = _checkContentOldValue;

            _currentGroupModeIndex = _currentGroupModeIndexOldValue;
            _isRollBack = false;
        }
    }
}
