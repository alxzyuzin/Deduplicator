using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;

namespace Deduplicator.Common
{
    public class FileSelectionOptions : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyname)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyname));
        }

        Settings _settings;

        public FileSelectionOptions()
        {
        }

        public void Init(Settings settings)
        {
            _settings = settings;
            AllFiles = true;
        } 

        private bool _allFiles = true;
        public bool AllFiles
        { get { return _allFiles; }
          set
          {
                if (_allFiles!=value)
                {
                    _allFiles = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AllFiles)));
                }
          }
        }
        public bool ImageFiles { get; set; } = false;
        public bool VideoFiles { get; set; } = false;
        public bool AudioFiles { get; set; } = false;

        public bool DocumentFiles { get; set; } = false;
        public bool DataFiles { get; set; } = false;
        public bool ExecutableFiles { get; set; } = false;
        public bool SystemFiles { get; set; } = false;
        public bool CompressedFiles { get; set; } = false;
        public bool AppSourceCodeFiles { get; set; } = false;



        private string _specialExtentions = string.Empty;
        public string SpecialExtentions
        {
            get { return _specialExtentions; }
            set
            {
                if (_specialExtentions != value)
                {
                    _specialExtentions = value;
                    NotifyPropertyChanged("SpecialExtentions");
                }
            }
        }

        public string ExcludeExtentions { get; set; } = string.Empty;

        public string AudioFileExtentions => _settings?.AudioFileExtentions;
        public string VideoFileExtentions => _settings?.VideoFileExtentions;
        public string ImageFileExtentions => _settings?.ImageFileExtentions;
        public string DocumentFileExtentions => _settings?.DocumentFileExtentions;
        public string DataFileExtentions => _settings?.DataFileExtentions;
        public string ExecutableFileExtentions => _settings?.ExecutableFileExtentions;
        public string SystemFileExtentions => _settings?.SystemFileExtentions;
        public string CompressedFileExtentions => _settings?.CompressedFileExtentions;
        public string AppSourceCodeFileExtentions => _settings?.AppSourceCodeFileExtentions;

        public List<string> FileTypeFilter
        {
            get
            {
                StringBuilder allExtentions = new StringBuilder();
                char delimiter = ';';
                List<string> filter = new List<string>();
                if (AllFiles)
                    return filter;

                if (VideoFiles)
                    allExtentions.Append(_settings.VideoFileExtentions);
                allExtentions.Append(delimiter);
                if (AudioFiles)
                    allExtentions.Append(_settings.AudioFileExtentions);
                allExtentions.Append(delimiter);
                if (ImageFiles)
                    allExtentions.Append(_settings.ImageFileExtentions);
                allExtentions.Append(delimiter);
                if (DocumentFiles)
                    allExtentions.Append(_settings.DocumentFileExtentions);
                allExtentions.Append(delimiter);
                if (DataFiles)
                    allExtentions.Append(_settings.DataFileExtentions);
                allExtentions.Append(delimiter);
                if (ExecutableFiles)
                    allExtentions.Append(_settings.ExecutableFileExtentions);
                allExtentions.Append(delimiter);
                if (SystemFiles)
                    allExtentions.Append(_settings.SystemFileExtentions);
                allExtentions.Append(delimiter);
                if (CompressedFiles)
                    allExtentions.Append(_settings.CompressedFileExtentions);
                allExtentions.Append(delimiter);
                if (AppSourceCodeFiles)
                    allExtentions.Append(_settings.AppSourceCodeFileExtentions);
                allExtentions.Append(delimiter);

                if (SpecialExtentions.Length > 0)
                    allExtentions.Append(SpecialExtentions);

                //var extArr = allExtentions.ToString().Split(';');
                filter.AddRange(allExtentions.ToString().Split(';').Where(f => f != ""));
                    
                

                return filter;
            }
        }
    }
}


