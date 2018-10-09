using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Deduplicator.Common
{
    public class Settings: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyname)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyname));
        }

        private string _imageFileExtentions = string.Empty;
        public string ImageFileExtentions
        {
            get { return _imageFileExtentions; }
            set
            {
                if (_imageFileExtentions != value)
                {
                    _imageFileExtentions=value;
                    NotifyPropertyChanged("ImageFileExtentions");
                }
            }
        }
        public string AudioFileExtentions { get; set; }
        public string VideoFileExtentions { get; set; }
        public bool InitialSetupDone { get; set; }

        public void Init()
        {

        }

        public void Restore()
        {
            Object value =null;

            ApplicationDataContainer LocalSettings = ApplicationData.Current.LocalSettings;

            value = LocalSettings.Values["InitialSetupDone"];
            InitialSetupDone =  (value != null) ? (bool)value : false;

            if (InitialSetupDone)
            {

                value = LocalSettings.Values["ImageFileExtentions"];
                ImageFileExtentions = (value != null) ? (string)value : string.Empty;

                value = LocalSettings.Values["AudioFileExtentions"];
                AudioFileExtentions = (value != null) ?(string)value : string.Empty;

                value = LocalSettings.Values["VideoFileExtentions"];
                VideoFileExtentions = (value != null) ? (string)value : string.Empty;
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
            
            LocalSettings.Values["ImageFileExtentions"] = ImageFileExtentions;
            LocalSettings.Values["AudioFileExtentions"] = AudioFileExtentions;
            LocalSettings.Values["VideoFileExtentions"] = VideoFileExtentions;
            InitialSetupDone = true;
            LocalSettings.Values["InitialSetupDone"] = InitialSetupDone;
            

        }

        public void SetDefault()
        {
            AudioFileExtentions = ".aif;.iff;.m3u;.m4a;.mid;.mp3;.mpa;.ra;.wav;.wma;.flac;";
            VideoFileExtentions = ".3g2;.3gp;.asf;.asx;.avi;.flv;.mov;.mp4;.mpg;.rm;.swf;.vob;.wmv;";
            ImageFileExtentions = ".3dm;.max;.bmp;.gif;.jpg;.jpeg;.png;.psd;.pspimage;.thm;.tif;.yuv;.ai;.drw;.eps;.ps;.svg;";
            InitialSetupDone = false;
        }
    }
}
