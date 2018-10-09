using System.ComponentModel;

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

        private bool _allFiles = true;
        public bool AllFiles
        {
            get { return _allFiles; }
            set
            {
                if (_allFiles!=value)
                {
                    _allFiles = value;
                    NotifyPropertyChanged("AllFiles");
                }
            }
        }
        public bool SpecificFiles { get; set; } = false;
        public bool VideoFiles { get; set; } = false;
        public bool AudioFiles { get; set; } = false;
        public bool ImageFiles { get; set; } = false;

        private bool _specialFiles = true;
        public bool SpecialFiles
        {
            get { return _specialFiles; }
            set
            {
                if (_specialFiles != value)
                {
                    _specialFiles = value;
                    NotifyPropertyChanged("SpecialFiles");
                }
            }
        }

        private string _specialExtentions = string.Empty;
        public string SpecialExtentions
        { get { return _specialExtentions; }
          set
            {
                if (_specialExtentions!=value)
                {
                    _specialExtentions = value;
                    NotifyPropertyChanged("SpecialExtentions");
                }
            }
        }

        public string ExcludeExtentions { get; set; } = string.Empty;


        private string _audioFileExtentions = ".aif;.iff;.m3u;.m4a;.mid;.mp3;.mpa;.ra;.wav;.wma;.flac;";
        public string AudioFileExtentions
        {   get { return _audioFileExtentions; }
            set
            {
                if (_audioFileExtentions!=value)
                {
                    _audioFileExtentions = value;
                    NotifyPropertyChanged("AudioFileExtentions");
                }
            }
        }

        private string _videoFileExtentions = ".3g2;.3gp;.asf;.asx;.avi;.flv;.mov;.mp4;.mpg;.rm;.swf;.vob;.wmv;";
        public string VideoFileExtentions
        { get { return _videoFileExtentions; }
          set
            {
                if (_videoFileExtentions != value)
                {
                    _videoFileExtentions = value;
                    NotifyPropertyChanged("VideoFileExtentions");
                }
            }
        }

        private string _imageFileExtentions = ".3dm;.max;.bmp;.gif;.jpg;.jpeg;.png;.psd;.pspimage;.thm;.tif;.yuv;.ai;.drw;.eps;.ps;.svg;";
        public string ImageFileExtentions
        { get { return _imageFileExtentions; }
          set
            {
                if (_imageFileExtentions != value)
                {
                    _imageFileExtentions = value;
                    NotifyPropertyChanged("ImageFileExtentions");
                }
            }
        }

 //       private string _requestedFileExtentions = string.Empty;

        public bool ExtentionRequested(string extention)
        {

            if (ExcludeExtentions.Contains(extention))
                return false;

            if (AllFiles)
                return true;
            
            if (VideoFiles & _videoFileExtentions.Contains(extention))
                return true;

            if (AudioFiles & _audioFileExtentions.Contains(extention))
                return true;

            if (ImageFiles & _imageFileExtentions.Contains(extention))
                return true;

            if (SpecialExtentions.Length > 0 & SpecialExtentions.Contains(extention))
                return true;

            return false;
        }
    }
}

/*
Text Files Types and Formats
.doc
Microsoft Word Document
.docx
Microsoft Word Open XML Document
.log
Log File
.msg
Outlook Mail Message
.pages
Pages Document
.rtf
Rich Text Format File
.txt
Plain Text File
.wpd
WordPerfect Document
.wps
Microsoft Works Word Processor Document


Data Files Types and Formats
.csv
Comma Separated Values File
.dat
Data File
.efx
eFax Document
.gbr
Gerber File
.key
Keynote Presentation
.pps
PowerPoint Slide Show
.ppt
PowerPoint Presentation
.pptx
PowerPoint Open XML Presentation
.sdf
Standard Data File
.tax2010
TurboTax 2010 Tax Return
.vcf
vCard File
.xml
XML File

Audio File Types and Formats

.aif
Audio Interchange File Format
.iff
Interchange File Format
.m3u
Media Playlist File
.m4a
MPEG-4 Audio File
.mid
MIDI File
.mp3
MP3 Audio File
.mpa
MPEG-2 Audio File
.ra
Real Audio File
.wav
WAVE Audio File
.wma
Windows Media Audio File

Video Files Types and Formats

.3g2
3GPP2 Multimedia File
.3gp
3GPP Multimedia File
.asf
Advanced Systems Format File
.asx
Microsoft ASF Redirector File
.avi
Audio Video Interleave File
.flv
Flash Video File
.mov
Apple QuickTime Movie
.mp4
MPEG-4 Video File
.mpg
MPEG Video File
.rm
Real Media File
.swf
Shockwave Flash Movie
.vob
DVD Video Object File
.wmv
Windows Media Video File

3D Image Files Types and Formats
.3dm
Rhino 3D Model
.max
3ds Max Scene File


Raster Image Files Types and Formats
.bmp
Bitmap Image File
.gif
Graphical Interchange Format File
.jpg
JPEG Image File
.png
Portable Network Graphic
.psd
Adobe Photoshop Document
.pspimage
PaintShop Pro Image
.thm
Thumbnail Image File
.tif
Tagged Image File
.yuv
YUV Encoded Image File

Vector Image Files Types and Formats
.ai
Adobe Illustrator File
.drw
Drawing File
.eps
Encapsulated PostScript File
.ps
PostScript File
.svg
Scalable Vector Graphics File

Page Layout Files Types and Formats
.indd
Adobe InDesign Document
.pct
Picture File
.pdf
Portable Document Format File
.qxd
QuarkXPress Document
.qxp
QuarkXPress Project File
.rels
Open Office XML Relationships File


Spreadsheet Files Types and Formats
.xlr
Works Spreadsheet
.xls
Excel Spreadsheet
.xlsx
Microsoft Excel Open XML Spreadsheet


Database Files Types and Formats
.accdb
Access 2007 Database File
.db
Database File
.dbf
Database File
.mdb
Microsoft Access Database
.pdb
Program Database
.sql
Structured Query Language Data

Executable Files Types and Formats
.app
Mac OS X Application
.bat
DOS Batch File
.cgi
Common Gateway Interface Script
.com
DOS Command File
.exe
Windows Executable File
.gadget
Windows Gadget
.jar
Java Archive File
.pif
Program Information File
.vb
VBScript File
.wsf
Windows Script File


Game Files Types and Formats
.gam
Saved Game File
.nes
Nintendo (NES) ROM File
.rom
N64 Game ROM File
.sav
Saved Game

CAD Files Types and Formats
.dwg
AutoCAD Drawing Database File
.dxf
Drawing Exchange Format File

GIS Files Types and Formats
.gpx
GPS Exchange File
.kml
Keyhole Markup Language File

Web Files Types and Formats
.asp
Active Server Page
.cer
Internet Security Certificate
.csr
Certificate Signing Request File
.css
Cascading Style Sheet
.htm
Hypertext Markup Language File
.html
Hypertext Markup Language File
.js
JavaScript File
.jsp
Java Server Page
.php
Hypertext Preprocessor File
.rss
Rich Site Summary
.xhtml
Extensible Hypertext Markup Language File


Plugin Files Types and Formats
.8bi
Photoshop Plug-in
.plugin
Mac OS X Plug-in
.xll
Excel Add-In File


Font Files Types and Formats
.fnt
Windows Font File
.fon
Generic Font File
.otf
OpenType Font
.ttf
TrueType Font

System Files Types and Formats
.cab
Windows Cabinet File
.cpl
Windows Control Panel Item
.cur
Windows Cursor
.dll
Dynamic Link Library
.dmp
Windows Memory Dump
.drv
Device Driver
.lnk
File Shortcut
.sys
Windows System File


Settings Files Types and Formats
.cfg
Configuration File
.ini
Windows Initialization File
.keychain
Mac OS X Keychain File
.prf
Outlook Profile File


Encoded Files Types and Formats
.bin
Macbinary Encoded File
.hqx
BinHex 4.0 Encoded File
.mim
Multi-Purpose Internet Mail Message File
.uue
Uuencoded File

Compressed Files Types and Formats
.7z
7-Zip Compressed File
.deb
Debian Software Package
.gz
Gnu Zipped Archive
.pkg
Mac OS X Installer Package
.rar
WinRAR Compressed Archive
.rpm
Red Hat Package Manager File
.sit
StuffIt Archive
.sitx
StuffIt X Archive
.tar.gz
Tarball File
.zip
Zipped File
.zipx
Extended Zip File


Disk Image Files Types and Formats
.dmg
Mac OS X Disk Image
.iso
Disc Image File
.toast
Toast Disc Image
.vcd
Virtual CD

Developer Files Types and Formats
.c
C/C++ Source Code File
.class
Java Class File
.cpp
C++ Source Code File
.cs
Visual C# Source Code File
.dtd
Document Type Definition File
.fla
Adobe Flash Animation
.java
Java Source Code File
.m
Objective-C Implementation File
.pl
Perl Script
.py
Python Script

Backup Files Types and Formats
.bak
Backup File
.gho
Norton Ghost Backup File
.ori
Original File
.tmp
Temporary File

Misc Files Types and Formats
.dbx
Outlook Express E-mail Folder
.msi
Windows Installer Package
.part
Partially Downloaded File
.torrent
BitTorrent File
*/

