using System.Collections.Generic;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Deduplicator.Common
{
    public class FileCompareOptions : INotifyPropertyChanged
    {
        private GroupingAttribute Content = new GroupingAttribute( @"Content",FileAttribs.Content, 5);
        private GroupingAttribute Name = new GroupingAttribute(@"Name", FileAttribs.Name, 1);
        private GroupingAttribute Size = new GroupingAttribute(@"Size", FileAttribs.Size, 4);
        private GroupingAttribute CreationDate = new GroupingAttribute(@"Creation date", FileAttribs.DateCreated, 2);
        private GroupingAttribute ModificationDate = new GroupingAttribute(@"Modification date", FileAttribs.DateModified, 3);
        private GroupingAttribute None = new GroupingAttribute(@"Do not group", FileAttribs.None, 0);

        private enum CheckBoxAction {Checked = 1, Unchecked = 0}
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyname)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyname));
        }

        private bool m_isRollBack = false;
        public bool IsRollBack
        { get { return m_isRollBack; } }

        private bool m_checkNameOldValue;
        private bool m_checkName;
        public bool CheckName
        {
            get { return m_checkName; }
            set
            {
                if (m_checkName != value)
                {
                    m_checkName = value;
                    UpdateGroupingModeList(Name, value ? CheckBoxAction.Checked : CheckBoxAction.Unchecked);
                    NotifyPropertyChanged("CheckName");
                }
            }
        }

        private bool m_checkSizeOldValue;
        private bool m_checkSize;
        public bool CheckSize
        {
            get { return m_checkSize; }
            set
            {
                if (m_checkSize != value)
                {
                    m_checkSize = value;
                    UpdateGroupingModeList(Size, value ? CheckBoxAction.Checked : CheckBoxAction.Unchecked);
                    NotifyPropertyChanged("CheckSize");
                }
            }
        }

        private bool m_checkCreationDateTimeOldValue;
        private bool m_checkCreationDateTime;
        public bool CheckCreationDateTime
        {
            get { return m_checkCreationDateTime; }
            set
            {
                if (m_checkCreationDateTime != value)
                {
                    m_checkCreationDateTime = value;
                    UpdateGroupingModeList(CreationDate, value ? CheckBoxAction.Checked : CheckBoxAction.Unchecked);
                    NotifyPropertyChanged("CheckCreationDateTime");
                }
            }
        }

        private bool m_checkModificationDateTimeOldValue;
        private bool m_checkModificationDateTime;
        public bool CheckModificationDateTime
        {
            get { return m_checkModificationDateTime; }
            set
            {
                if (m_checkModificationDateTime != value)
                {
                    m_checkModificationDateTime = value;
                    UpdateGroupingModeList(ModificationDate, value ? CheckBoxAction.Checked : CheckBoxAction.Unchecked);
                    NotifyPropertyChanged("CheckModificationDateTime");
                }
            }
        }

        private bool m_checkContentOldValue;
        private bool m_checkContent;
        public bool CheckContent
        {
            get { return m_checkContent; }
            set
            {
                if (m_checkContent != value)
                {
                    m_checkContent = value;
                    UpdateGroupingModeList(Content, value? CheckBoxAction.Checked : CheckBoxAction.Unchecked);
                    NotifyPropertyChanged("CheckContent");
                }
            }
        }

        private ObservableCollection<GroupingAttribute> m_grouppingAttributes = new ObservableCollection<GroupingAttribute>();
        public ObservableCollection<GroupingAttribute> GrouppingAttributes { get { return m_grouppingAttributes; } }

        private GroupingAttribute m_selectedGroupAttrib;
        public GroupingAttribute SelectedGroupAttrib
        {
            get
            {
                return m_selectedGroupAttrib;
            }
                
            set
            {
                if (m_selectedGroupAttrib != value)
                    m_selectedGroupAttrib = value;
            }
        }

        private int m_currentGroupModeIndexOldValue;
        private int m_currentGroupModeIndex = -1;
        public  int CurrentGroupModeIndex
        {
            get { return m_currentGroupModeIndex; }
            set
            {
                if (m_currentGroupModeIndex != value)
                {
                    m_currentGroupModeIndex = value;
                    NotifyPropertyChanged("CurrentGroupModeIndex");
                }
            }
        }

 
        public FileCompareOptions()
        {
            m_grouppingAttributes.Add(None);
            CurrentGroupModeIndex = -1;
            CheckName = true;
            CheckSize = true;
            m_checkNameOldValue = m_checkName;
            m_checkSizeOldValue = m_checkSize;
            m_checkCreationDateTimeOldValue = m_checkCreationDateTime;
            m_checkModificationDateTimeOldValue = m_checkModificationDateTime;
            m_checkContentOldValue = m_checkContent;
            m_currentGroupModeIndexOldValue = m_currentGroupModeIndex;

            m_selectedGroupAttrib = new GroupingAttribute();

        }

        private void UpdateGroupingModeList(GroupingAttribute attrib, CheckBoxAction action)
        {
            if (action == CheckBoxAction.Checked)
                m_grouppingAttributes.Add(attrib);
            else
            {
                m_grouppingAttributes.Remove(attrib);
                CurrentGroupModeIndex = 0;
            }
            NotifyPropertyChanged("ResultGrouppingModesList");
         }

          public void Commit()
        {
            m_checkNameOldValue = m_checkName;
            m_checkSizeOldValue = m_checkSize;
            m_checkCreationDateTimeOldValue = m_checkCreationDateTime;
            m_checkModificationDateTimeOldValue = m_checkModificationDateTime;
            m_checkContentOldValue = m_checkContent;
        }

        public void RollBack()
        {
            m_isRollBack = true;
            if (m_checkName != m_checkNameOldValue)
                CheckName = m_checkNameOldValue;
            if (m_checkSize != m_checkSizeOldValue)
                CheckSize = m_checkSizeOldValue;
            if (m_checkCreationDateTime != m_checkCreationDateTimeOldValue)
                CheckCreationDateTime = m_checkCreationDateTimeOldValue;
            if (m_checkModificationDateTime != m_checkModificationDateTimeOldValue)
                CheckModificationDateTime = m_checkModificationDateTimeOldValue;
            if (m_checkContent != m_checkContentOldValue)
                CheckContent = m_checkContentOldValue;
            m_currentGroupModeIndex = m_currentGroupModeIndexOldValue;
            m_isRollBack = false;
        }

 
    } // Class FileCompareOptions

    public class GroupingAttribute
    {
        public GroupingAttribute() { }
        public GroupingAttribute(string name, FileAttribs attribute, int weight)
        {
            Name = name;
            Attribute = attribute;
            Weight = weight;

        }
        public string Name { get; set; } = string.Empty;
        public FileAttribs Attribute { get; set; } = FileAttribs.Undefined;
        public int Weight { get; set; } = 0;
 
        public override string ToString()
        {
            return Name;
        } // Class GroupingAttribute
    }
}   // Namespace Deduplicator.Common


