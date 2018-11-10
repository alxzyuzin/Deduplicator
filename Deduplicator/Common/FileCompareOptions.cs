using System.Collections.Generic;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Deduplicator.Common
{
    public class FileCompareOptions : INotifyPropertyChanged
    {
        private const string Content          = @"Content";
        private const string Name             = @"Name";
        private const string Size             = @"Size";
        private const string CreationDate     = @"Creation date";
        private const string ModificationDate = @"Modification date";
        private const string None             = @"Do not group";

        private enum CheckBoxAction {Checked = 1, Unchecked = 0}
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
                    UpdateGroupingModeList(new GroupingAttribute(Name, FileAttribs.Name),
                       value ? CheckBoxAction.Checked : CheckBoxAction.Unchecked);
                    NotifyPropertyChanged("CheckName");
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
                    UpdateGroupingModeList(new GroupingAttribute(Size, FileAttribs.Size),
                                           value ? CheckBoxAction.Checked : CheckBoxAction.Unchecked);
                    NotifyPropertyChanged("CheckSize");
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
                    UpdateGroupingModeList(new GroupingAttribute(CreationDate, FileAttribs.DateCreated),
                                           value ? CheckBoxAction.Checked : CheckBoxAction.Unchecked);

                    NotifyPropertyChanged("CheckCreationDateTime");
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
                    UpdateGroupingModeList(new GroupingAttribute(ModificationDate, FileAttribs.DateModified),
                        value ? CheckBoxAction.Checked : CheckBoxAction.Unchecked);

                    NotifyPropertyChanged("CheckModificationDateTime");
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
                    UpdateGroupingModeList( new GroupingAttribute(Content,FileAttribs.Content),
                                            value? CheckBoxAction.Checked : CheckBoxAction.Unchecked);
                    NotifyPropertyChanged("CheckContent");
                }
            }
        }

        private ObservableCollection<GroupingAttribute> _grouppingAttributes = new ObservableCollection<GroupingAttribute>();
        public ObservableCollection<GroupingAttribute> GrouppingAttributes { get { return _grouppingAttributes; } }

        public FileAttribs CurrentGroupModeAttrib
        {
            get
            {
                if ((_currentGroupModeIndex >= 0) && (_currentGroupModeIndex < _grouppingAttributes.Count))
                    return _grouppingAttributes[_currentGroupModeIndex].Attribute;
                else
                    return FileAttribs.None;
            }
        }

        private int _currentGroupModeIndexOldValue;
        private int _currentGroupModeIndex = -1;
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
            _grouppingAttributes.Add(new GroupingAttribute(None, FileAttribs.None));
            CheckName = true;
            CheckSize = true;
            CurrentGroupModeIndex = -1; 
            _checkNameOldValue = _checkName;
            _checkSizeOldValue = _checkSize;
            _checkCreationDateTimeOldValue = _checkCreationDateTime;
            _checkModificationDateTimeOldValue = _checkModificationDateTime;
            _checkContentOldValue = _checkContent;
            _currentGroupModeIndexOldValue = _currentGroupModeIndex;
        }

        private void UpdateGroupingModeList(GroupingAttribute attrib, CheckBoxAction action)
        {
            if (action == CheckBoxAction.Checked)
                _grouppingAttributes.Add(attrib);
            else
                foreach (var item in _grouppingAttributes)
                    if (item.Attribute == attrib.Attribute)
                    {
                        _grouppingAttributes.Remove(item);
                        break;
                    }

            if (_grouppingAttributes.Count > 0)
                CurrentGroupModeIndex = 0;
            NotifyPropertyChanged("ResultGrouppingModesList");
         }

        private void UpdateGroupingModeList()
        {
 
        }

        public void Commit()
        {
            _checkNameOldValue = _checkName;
            _checkSizeOldValue = _checkSize;
            _checkCreationDateTimeOldValue = _checkCreationDateTime;
            _checkModificationDateTimeOldValue = _checkModificationDateTime;
            _checkContentOldValue = _checkContent;
//            UpdateGroupingModeList();
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
//            UpdateGroupingModeList();
            _currentGroupModeIndex = _currentGroupModeIndexOldValue;
            _isRollBack = false;
        }
    } // Class FileCompareOptions

    public class GroupingAttribute
    {
        public GroupingAttribute(string name, FileAttribs attribute)
        {
            Name = name;
            Attribute = attribute;
        }
        public string Name { get; set; }
        public FileAttribs Attribute { get; set; }

        public override string ToString()
        {
            return Name;
        } // Class GroupingAttribute
    }
}   // Namespace Deduplicator.Common


