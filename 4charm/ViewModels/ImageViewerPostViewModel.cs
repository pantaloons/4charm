using _4charm.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace _4charm.ViewModels
{
    class ImageViewerPostViewModel : ViewModelBase
    {
        public ulong Number
        {
            get { return GetProperty<ulong>(); }
            set { SetProperty(value); }
        }

        public ulong RenamedFileName
        {
            get { return GetProperty<ulong>(); }
            set { SetProperty(value); }
        }

        public ICommand UpdateProgress
        {
            get { return GetProperty<ICommand>(); }
            set { SetProperty(value); }
        }

        public bool IsDownloading
        {
            get { return GetProperty<bool>(); }
            set { SetProperty(value); }
        }

        public bool IsSelected
        {
            get { return GetProperty<bool>(); }
            set { SetProperty(value); }
        }

        public int DownloadProgress
        {
            get { return GetProperty<int>(); }
            set { SetProperty(value); }
        }

        public Uri ThumbnailSrc
        {
            get { return GetProperty<Uri>(); }
            set { SetProperty(value); }
        }

        public Uri ImageSrc
        {
            get { return GetProperty<Uri>(); }
            set { SetProperty(value); }
        }

        public double AspectRatio
        {
            get { return GetProperty<double>(); }
            set { SetProperty(value); }
        }

        public Models.API.APIPost.FileTypes FileType
        {
            get { return GetProperty<Models.API.APIPost.FileTypes>(); }
            set { SetProperty(value); }
        }

        public ImageViewerPostViewModel(Post p)
        {
            Number = p.Number;
            RenamedFileName = p.RenamedFileName;
            ThumbnailSrc = p.ThumbnailSrc;
            ImageSrc = p.ImageSrc;
            AspectRatio = p.ImageWidth / (double)p.ImageHeight;
            FileType = p.FileType;

            UpdateProgress = new ModelCommand<int>(DoUpdateProgress);
            IsDownloading = true;
        }

        private void DoUpdateProgress(int progress)
        {
            DownloadProgress = progress;
            if (progress == 100)
            {
                IsDownloading = false;
            }
        }
    }
}
