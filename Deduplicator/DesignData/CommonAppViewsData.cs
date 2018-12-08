using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Deduplicator.DesignData
{
    public class CommonAppViewsData
    {
        private ApplicationViews.CmdButtons CmdButtonsVisualState { get; set; }
        public Visibility ViewWhereToSearchVisibility { get; set; }
        public Visibility ViewSearchResultsVisibility { get; set; }
        public Visibility ViewSettingsVisibility { get; set; }
        public Visibility ViewOptionsVisibility { get; set; }
        public Visibility BtnDelSelectedFilesVisibility { get; set; }
        public Visibility BtnStartSearchVisibility { get; set; }
        public Visibility BtnCancelSearchVisibility { get; set; }
        public Visibility BtnSaveSettingsVisibility { get; set; }
        public Visibility BtnAddFolderVisibility { get; set; }
        public Visibility BtnDelFolderVisibility { get; set; }
        public Visibility GroupingSelectorVisibility { get; set; }
        public Visibility GroupByPrimaryFolderVisibility { get; set; }
        private Visibility EmptyContentMessageVisibility { get; set; }
        public bool BtnAddFolderEnabled { get; set; }
        public bool BtnDelFolderEnabled { get; set; }
        public bool BtnStartSearchEnabled { get; set; }
        public bool BtnStopSearchEnabled { get; set; }
        public bool BtnDelFilesEnabled { get; set; }
        public bool GroupingModeSelectorEnabled { get; set; }
        public string TabHeader { get; set; }
        public string EmptyContentMessage { get; set; }

        public FileCompareOptions FileCompareOptions { get; set; }

        public List<GroupAttribute> GrouppingAttributes { get; set; }

        public AppData AppData { get; set; }

        public CommonAppViewsData()
        {
            FileCompareOptions = new FileCompareOptions();
        }

    } // Class ComonAppViewsData

    public class FileSelectionOptions
    {
        public FileSelectionOptions() { }

        public bool AllFiles   { get; set; } = false;
        public bool VideoFiles { get; set; } = false;
        public bool AudioFiles { get; set; } = false;
        public bool ImageFiles { get; set; } = false;

        public bool DocumentFiles { get; set; } = false;
        public bool DataFiles { get; set; } = false;
        public bool ExecutableFiles { get; set; } = false;
        public bool SystemFiles { get; set; } = false;
        public bool CompressedFiles { get; set; } = false;
        public bool AppSourceCodeFiles { get; set; } = false;
        
        public string SpecialExtentions { get; set; }
        public string ExcludeExtentions { get; set; } = string.Empty;
    }

    public class FileCompareOptions
    {
       
        public bool IsRollBack { get; set; }
        public bool CheckName { get; set; }
        public bool CheckSize { get; set; }
        public bool CheckCreationDateTime { get; set; }
        public bool CheckModificationDateTime { get; set; }
        public bool CheckContent { get; set; }
    //    public GroupAttribute CurrentGroupModeAttrib { get; set; }
        public int CurrentGroupModeIndex { get; set; }
        public ObservableCollection<GroupAttribute> GrouppingAttributes { get; set; }

        public FileCompareOptions()
            {
                //GrouppingAttributes = new ObservableCollection<GroupAttribute>
                //{

                //    new GroupAttribute {GroupName= "Do not group files", Attribute= Common.FileAttribs.None },
                //    new GroupAttribute {GroupName= "Name", Attribute= Common.FileAttribs.Name },
                //    new GroupAttribute {GroupName= "Size", Attribute= Common.FileAttribs.Size },
                //    new GroupAttribute {GroupName= "Content", Attribute= Common.FileAttribs.Content }
                //};

            }


    } // Class FileCompareOptions

    public class GroupAttribute
    {
        public string GroupName { get; set; }
        public Common.FileAttribs Attribute { get; set; }

        public new string ToString()
        {
            return GroupName;
        }
    }

    public class AppData
    {
        public List<Folder> Folders { get; set; }
        public GroupedFilesCollection DuplicatedFiles { get; set; }
        public Common.DataModel.SearchStatus Status { get; set; }
        public int FoldersCount { get; set; }
        public int DuplicatesCount { get; set; }
        public bool PrimaryFolderSelected { get; set; }
        public bool OperationCompleted { get; set; }
        public Settings Settings { get; set; }

        public FileSelectionOptions FileSelectionOptions { get; set; }
        public FileCompareOptions FileCompareOptions { get; set; }
    } // Class DataModel

    
    public class Folder
    {
        public string FullName { get; set; }
        public bool IsPrimary { get; set; }
        public String IsPrimaryName { get; set; }
        public bool SearchInSubfolders { get; set; }
        public bool Protected { get; set; } = false;
        public String AccessToken { get; set; }
    } // Class Folder

    public class Settings
    {
        public string AudioFileExtentions
        {
            get { return ".aif;.iff;.m3u;.m4a;.mid;.mp3;.mpa;.ra;.wav;.wma;.flac;"; }
        }
        public string VideoFileExtentions { get; set; }
        public string ImageFileExtentions { get; set; }

        public Settings()
        {
            //           this.AudioFileExtentions = ".aif;.iff;.m3u;.m4a;.mid;.mp3;.mpa;.ra;.wav;.wma;.flac;";
            this.VideoFileExtentions = ".3g2;.3gp;.asf;.asx;.avi;.flv;.mov;.mp4;.mpg;.rm;.swf;.vob;.wmv;";
            this.ImageFileExtentions = ".3dm;.max;.bmp;.gif;.jpg;.jpeg;.png;.psd;.pspimage;.thm;.tif;.yuv;.ai;.drw;.eps;.ps;.svg;";
        }
    } //  Class DTSettings

    public class GroupedFilesCollection:ObservableCollection<FilesGroup>
    {
        public GroupedFilesCollection()
        {
            Add(new FilesGroup {GroupName ="Const group name", TotalSize=98789787, FileSize=9878777 });

        }
    }

    public class FilesGroup : IEnumerable<File>
    {
        public string GroupName { get; set; } = "Groupe name";
        public ulong TotalSize { get; set; } = 8905677;
        public ulong FileSize { get; set; } = 975445678;
        public bool IsChecked { get; set; }
        private List<File> _files = new List<File>();

        public FilesGroup()
        {
            _files.Add(new File { Name = "File name", FileType = ".xaml", DateCreated = new DateTime(2018, 12, 25) });
        }

        public IEnumerator<File> GetEnumerator()
        {
            return ((IEnumerable<File>)_files).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<File>)_files).GetEnumerator();
        }

        public void Add(File file)
        {
            _files.Add(file);
        }
    }

    public sealed class File
    {
        public string Name { get; set; } = "File name";
        public string FileType { get; set; } = ".xaml";
        public string FullName { get; set; } = "File full name";
        public string Path { get; set; } = "File path";
        public DateTime DateCreated { get; set; } = new DateTime(2018,12,25);
        public DateTime DateModifyed { get; set; } = new DateTime(2018, 2, 5);
        public ulong Size { get; set; } = 1111110;
        public bool FromPrimaryFolder { get; set; } = false;
        public string Extention { get; set; } = ".tyg";
        public bool IsProtected { get; set; } = false;
        public string ProtectionStatus { get; set; }

        public File()
        { }
    }  // Class File
}
