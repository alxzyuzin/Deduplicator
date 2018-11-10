using System.Collections.Generic;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Deduplicator.Common
{
    public class FileCompareOptions : INotifyPropertyChanged
    {
        private GroupingAttribute Content = new GroupingAttribute( @"Content",FileAttribs.Content);
        private GroupingAttribute Name = new GroupingAttribute(@"Name", FileAttribs.Name);
        private GroupingAttribute Size = new GroupingAttribute(@"Size", FileAttribs.Size);
        private GroupingAttribute CreationDate = new GroupingAttribute(@"Creation date", FileAttribs.DateCreated);
        private GroupingAttribute ModificationDate = new GroupingAttribute(@"Modification date", FileAttribs.DateModified);
        private GroupingAttribute None = new GroupingAttribute(@"Do not group", FileAttribs.None);

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
                    UpdateGroupingModeList(Name, value ? CheckBoxAction.Checked : CheckBoxAction.Unchecked);
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
                    UpdateGroupingModeList(Size, value ? CheckBoxAction.Checked : CheckBoxAction.Unchecked);
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
                    UpdateGroupingModeList(CreationDate, value ? CheckBoxAction.Checked : CheckBoxAction.Unchecked);
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
                    UpdateGroupingModeList(ModificationDate, value ? CheckBoxAction.Checked : CheckBoxAction.Unchecked);
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
                    UpdateGroupingModeList(Content, value? CheckBoxAction.Checked : CheckBoxAction.Unchecked);
                    NotifyPropertyChanged("CheckContent");
                }
            }
        }

        private ObservableCollection<GroupingAttribute> _grouppingAttributes = new ObservableCollection<GroupingAttribute>();
        public ObservableCollection<GroupingAttribute> GrouppingAttributes { get { return _grouppingAttributes; } }

        private GroupingAttribute _currentGroupModeAttrib;
        public GroupingAttribute CurrentGroupModeAttrib
        {
            get
            {
                return _currentGroupModeAttrib;
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
            _grouppingAttributes.Add(None);
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
            {
//                int i = _grouppingAttributes.IndexOf(CurrentGroupModeAttrib);
                _grouppingAttributes.Remove(attrib);
                CurrentGroupModeIndex = 0; // _grouppingAttributes.IndexOf(CurrentGroupModeAttrib); 
            }
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


