using System;
using System.ComponentModel;
using Windows.Storage;

namespace Deduplicator.Common
{
    public class Settings: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _imageFileExtentions = string.Empty;
        public string ImageFileExtentions
        {
            get { return _imageFileExtentions; }
            set
            {
                if (_imageFileExtentions != value)
                {
                    _imageFileExtentions=value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ImageFileExtentions)));
                }
            }
        }

        private string _audioFileExtentions;
        public string AudioFileExtentions
        {
            get { return _audioFileExtentions; }
            set
            {
                if (_audioFileExtentions != value)
                {
                    _audioFileExtentions = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AudioFileExtentions)));
                }
            }
        }

        private string _videoFileExtentions;
        public string VideoFileExtentions
        {
            get { return _videoFileExtentions; }
            set
            {
                if (_videoFileExtentions != value)
                {
                    _videoFileExtentions = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(VideoFileExtentions)));
                }
            }
        }

        private string _documentFileExtentions;
        public string DocumentFileExtentions
        {
            get { return _documentFileExtentions; }
            set
            {
                if (_documentFileExtentions != value)
                {
                    _documentFileExtentions = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DocumentFileExtentions)));
                }
            }
        }

        private string _dataFileExtentions;
        public string DataFileExtentions
        {
            get { return _dataFileExtentions; }
            set
            {
                if (_dataFileExtentions != value)
                {
                    _dataFileExtentions = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DataFileExtentions)));
                }
            }
        }

        private string _executableFileExtentions;
        public string ExecutableFileExtentions
        {
            get { return _executableFileExtentions; }
            set
            {
                if (_executableFileExtentions != value)
                {
                    _executableFileExtentions = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ExecutableFileExtentions)));
                }
            }
        }

        private string _systemFileExtentions;
        public string SystemFileExtentions
        {
            get { return _systemFileExtentions; }
            set
            {
                if (_systemFileExtentions != value)
                {
                    _systemFileExtentions = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SystemFileExtentions)));
                }
            }
        }

        private string _compressedFileExtentions;
        public string CompressedFileExtentions
        {
            get { return _compressedFileExtentions; }
            set
            {
                if (_compressedFileExtentions != value)
                {
                    _compressedFileExtentions = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CompressedFileExtentions)));
                }
            }
        }

        private string _appSourceCodeFileExtentions;
        public string AppSourceCodeFileExtentions
        {
            get { return _appSourceCodeFileExtentions; }
            set
            {
                if (_appSourceCodeFileExtentions != value)
                {
                    _appSourceCodeFileExtentions = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AppSourceCodeFileExtentions)));
                }
            }
        }

        public bool InitialSetupDone { get; set; }

        public void Restore()
        {
            Object value =null;

            ApplicationDataContainer LocalSettings = ApplicationData.Current.LocalSettings;

            value = LocalSettings.Values[nameof(InitialSetupDone)];
            InitialSetupDone = value !=null  ? true: false;

            if (InitialSetupDone)
            {
                ImageFileExtentions = LocalSettings.Values[nameof(ImageFileExtentions)] as string ?? string.Empty;
                AudioFileExtentions = LocalSettings.Values[nameof(AudioFileExtentions)] as string ?? string.Empty;
                VideoFileExtentions = LocalSettings.Values[nameof(VideoFileExtentions)] as string ?? string.Empty;
                DocumentFileExtentions = LocalSettings.Values[nameof(DocumentFileExtentions)] as string ?? string.Empty;
                DataFileExtentions = LocalSettings.Values[nameof(DataFileExtentions)] as string ?? string.Empty;
                ExecutableFileExtentions = LocalSettings.Values[nameof(ExecutableFileExtentions)] as string ?? string.Empty;
                SystemFileExtentions = LocalSettings.Values[nameof(SystemFileExtentions)] as string ?? string.Empty;
                CompressedFileExtentions = LocalSettings.Values[nameof(CompressedFileExtentions)] as string ?? string.Empty;
                AppSourceCodeFileExtentions = LocalSettings.Values[nameof(AppSourceCodeFileExtentions)] as string ?? string.Empty;
            }
            else
            {
                SetDefault();
            }
        }

        public void Save()
        {
            InitialSetupDone = false;
            ApplicationDataContainer LocalSettings = ApplicationData.Current.LocalSettings;
            
            LocalSettings.Values[nameof(ImageFileExtentions)] = ImageFileExtentions;
            LocalSettings.Values[nameof(AudioFileExtentions)] = AudioFileExtentions;
            LocalSettings.Values[nameof(VideoFileExtentions)] = VideoFileExtentions;
            LocalSettings.Values[nameof(DocumentFileExtentions)] = DocumentFileExtentions;
            LocalSettings.Values[nameof(DataFileExtentions)] = DataFileExtentions;
            LocalSettings.Values[nameof(ExecutableFileExtentions)] = ExecutableFileExtentions;
            LocalSettings.Values[nameof(SystemFileExtentions)] = SystemFileExtentions;
            LocalSettings.Values[nameof(CompressedFileExtentions)] = CompressedFileExtentions;
            LocalSettings.Values[nameof(AppSourceCodeFileExtentions)] = AppSourceCodeFileExtentions;


            InitialSetupDone = true;
            LocalSettings.Values[nameof(InitialSetupDone)] = InitialSetupDone;
            

        }

        public void SetDefault()
        {
            AudioFileExtentions = ".aif;.iff;.m3u;.m4a;.mid;.mp3;.mpa;.ra;.wav;.wma;.flac;";
            VideoFileExtentions = ".3g2;.3gp;.asf;.asx;.avi;.flv;.mov;.mp4;.mpg;.rm;.swf;.vob;.wmv;";
            ImageFileExtentions = ".3dm;.max;.bmp;.gif;.jpg;.jpeg;.png;.psd;.pspimage;.thm;.tif;.yuv;.ai;.drw;.eps;.ps;.svg;";
            DocumentFileExtentions = ".doc;.docx;.log;.msg;.pages;.rtf;.txt;.wpd;.wps;.xml;.xlr;.xls;.xlsx;.xll;.pps;.ppt;.pptx;.key;.dwg;.dxf;.gpx;.kml;";
            DataFileExtentions = ".csv;.dat;.efx;.gbr;.sdf;.accdb;.db;.dbf;.mdb;.pdb;.sql;";
            ExecutableFileExtentions = ".app;.bat;.cgi;.com;.exe;.gadget;.jar;.pif;.vb;.wsf;";
            SystemFileExtentions = ".cab;.cpl;.cur;.dll;.dmp;.drv;.lnk;.sys;.cfg;.ini;.prf;";
            CompressedFileExtentions = ".7z;.deb;.gz;.pkg;.rar;.rpm;.sit;.sitx;.tar;.gz;.zip;.zipx;.dmg;.iso;.toast;.vcd;.bak;.gho;.ori;.tmp;";
            AppSourceCodeFileExtentions = ".asp;.cer;.csr;.css;.htm;.html;.js;.jsp;.php;.rss;.xhtml;.c;.class;.cpp;.cs;.dtd;.fla;.java;.m;.pl;.py;";
            InitialSetupDone = false;
            
        }
    }
}

