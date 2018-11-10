using System;
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
        Hash = 256
    }

    public sealed class File
    {
        public string Name { get; set; } = string.Empty;
        public string FileType { get; set; } 
        public string FullName { get; set; } = string.Empty;
        public string Path
        {
            get
            {
                return FullName.Substring(0, FullName.IndexOf(Name)-1);
            }
        }
        public DateTime DateCreated { get; set; } = DateTime.Today;
        public DateTime DateModifyed { get; set; } = DateTime.Today;
        public ulong Size { get; set; } = 0;
//        public bool Duplicated { get; set; } = false; // Проверить. Возможно бесполезное свойство
        public bool FromPrimaryFolder { get; set; } = false;
        public string Extention
        {
            get
            {
                string res = string.Empty;
                if (Name.Contains("."))
                {
                    string[] filenameparts = Name.Split('.');
                    res = filenameparts[filenameparts.Length - 1];
                }
                return "."+res.Trim();
            }
        }
        public bool IsProtected { get; set; } = false;
        public string Protected { get { return IsProtected ? "Protected" : ""; } }

        public File()
        { }

        public File( string name, string fullname, string filetype, 
            DateTime datecreated, DateTime datemodifyed, ulong size, bool primaryfolder, bool protectedfile)
        {
            Name = name;
            FileType = filetype;
            FullName = fullname;
            DateCreated = datecreated;
            DateModifyed = datemodifyed;
            Size = size;
            FromPrimaryFolder = primaryfolder;
            IsProtected = protectedfile;
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
            catch (EndOfStreamException e)
            {
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

        public async Task<bool> IsEqualTo(File file, FileAttribs options)
        {
            switch(options)
            {
                case FileAttribs.Name:
                    if (string.Compare(this.Name, file.Name, StringComparison.OrdinalIgnoreCase) != 0)
                        return false;
                    break;

                case FileAttribs.DateCreated:
                    if (this.DateCreated != file.DateCreated)
                        return false;
                    break;
                case FileAttribs.DateModified:
                    if (this.DateModifyed != file.DateModifyed)
                        return false;
                    break;
                case FileAttribs.Content:
                    if (this.Size != file.Size)
                         return false;
                     else
                         if (await CompareFileContent(this, file) != 0)
                            return false;
                     break;
                case FileAttribs.Size:
                    if (this.Size!=file.Size)
                        return false;
                    break;
            }
            return true;
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
                    int i = await CompareFileContent(this, file);
                    return i;
                case FileAttribs.Size:
                    return this.Size.CompareTo(file.Size);
            }
            return 0;
        }
    }


}
