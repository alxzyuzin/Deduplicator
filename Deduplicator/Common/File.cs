using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Deduplicator.Common
{
    [Flags]
    public enum FileAttribs
    {
        None=0,
        Name =1,
        FileType = 2,
        Path = 4,
        DateCreated = 8,
        DateModified = 16,
        Size =32,
        Content=64,
        Extention=128,
        Hash = 256,
        Undefined = 512
    }

    public sealed class File:INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        public string Name { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Path => FullName.Substring(0, FullName.IndexOf(Name) - 1);
        public DateTime DateCreated { get; set; } = DateTime.Today;
        public DateTime DateModifyed { get; set; } = DateTime.Today;
        public ulong Size { get; set; } = 0;
        public bool FromPrimaryFolder { get; set; } = false;
        public string Extention
        {
            get
            {
                var res = string.Empty;
                if (Name.Contains("."))
                {
                    string[] filenameparts = Name.Split('.');
                    res = filenameparts[filenameparts.Length - 1];
                }
                return "."+res.Trim();
            }
        }
        private bool _isProtected = false;
        public bool IsProtected
        {
            get { return _isProtected; }
            set
            {
                if (_isProtected != value)
                {
                    _isProtected = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsProtected)));
                }
            }
        }
        public string ProtectionStatus => IsProtected ? "Protected" : "";
        private bool _isChecked = false;
        public bool IsChecked
        {
            get { return _isChecked; }
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
                }

            }
        }

        public File()
        { }

        public File( string name, string fullName, string fileType, 
            DateTime dateCreated, DateTime dateModifyed, ulong size, bool primaryFolder, bool isProtected)
        {
            Name = name;
            FileType = fileType;
            FullName = fullName;
            DateCreated = dateCreated;
            DateModifyed = dateModifyed;
            Size = size;
            FromPrimaryFolder = primaryFolder;
            IsProtected = isProtected;
        }

        private async Task<int> CompareFileContent(File file1, File file2)
        {
            StorageFile f1 = await StorageFile.GetFileFromPathAsync(file1.FullName);
            Stream s1 = (await f1.OpenReadAsync()).AsStreamForRead();
            BinaryReader r1 = new BinaryReader(s1);

            StorageFile f2 = await StorageFile.GetFileFromPathAsync(file2.FullName);
            Stream s2 = (await f2.OpenReadAsync()).AsStreamForRead();
            BinaryReader r2 = new BinaryReader(s2);

            try
            {
                while (true)
                {
                    UInt64 n1 = r1.ReadUInt64();
                    UInt64 n2 = r2.ReadUInt64();

                    if (n1 < n2)
                        return -1;
                    if (n1 > n2)
                        return 1;
                }
            }
            catch (EndOfStreamException ex)
            {
               string e= ex.Message;
                return 0;
            }
            finally
            {
                r1.Dispose();
                s1.Dispose();
                r2.Dispose();
                s2.Dispose();
            }
        }

        public async Task<int> CompareTo(File file, FileAttribs option)
        {
            switch (option)
            {
                case FileAttribs.Name:
                    return this.Name.CompareTo(file.Name);
                case FileAttribs.DateCreated:
                    return this.DateCreated.CompareTo(file.DateCreated);
                case FileAttribs.DateModified:
                    return this.DateModifyed.CompareTo(file.DateModifyed);
                case FileAttribs.Content:
                    if (this.Size < file.Size)
                        return -1;
                    if (this.Size > file.Size)
                        return 1;
                    //int i = await CompareFileContent(this, file);
                    return await CompareFileContent(this, file); //i;
                case FileAttribs.Size:
                    return this.Size.CompareTo(file.Size);
            }
            return 0;
        }

        public  File Clone()
        {
            return new File
            {
                Name = this.Name,
                FileType = this.FileType,
                FullName = this.FullName,
                DateCreated = this.DateCreated,
                DateModifyed = this.DateModifyed,
                Size = this.Size,
                FromPrimaryFolder = this.FromPrimaryFolder,
                IsProtected = this.IsProtected
            };
        }
    }


}
