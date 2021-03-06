﻿using System;
using System.ComponentModel;


namespace Deduplicator.Common
{
    public sealed class Folder : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

 #region Properties

        public string FullName { get; set; } = String.Empty;

        bool _isPrimary = false;
        public bool IsPrimary
        {
            get
            {
                return _isPrimary;
            }

            set
            {
                if (_isPrimary != value)
                {
                    _isPrimary = value;
                    NotifyPropertyChanged("IsPrimary");
                    NotifyPropertyChanged("IsPrimaryName");
                }
            }
        }

        public String IsPrimaryName
        {
            get
            {
                return (_isPrimary) ? "Primary folder" : "";
            }
        }

        public bool SearchInSubfolders { get; set; } = true;

        public bool IsProtected { get; set; } = false;

        public String AccessToken { get; set; }

#endregion

#region Constructors

        public Folder()
        {
            FullName = String.Empty;
        }

        public Folder(string fullName, bool isPrimary, bool searchInSubfolders, bool isProtected)
        {
            FullName = fullName;
            IsPrimary = isPrimary;
            SearchInSubfolders = searchInSubfolders;
            IsProtected = isProtected;
        }

        public Folder(string fullname, bool isprimary, String accesstoken)
        {
            FullName = fullname;
            IsPrimary = isprimary;
            AccessToken = accesstoken;
        }
#endregion

        public Folder Clone()
        {
            return new Folder
            {
                FullName = FullName,
                IsPrimary = IsPrimary,
                SearchInSubfolders = SearchInSubfolders,
                IsProtected = IsProtected,
                AccessToken = AccessToken
            };
        }
    }
}
