using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;

namespace _4charm.ViewModels
{
    public abstract class ImageViewModelBase : ViewModelBase
    {
        public BitmapImage Image
        {
            get { return GetProperty<BitmapImage>(); }
            set { SetProperty(value); }
        }

        private bool isImageRequested;
        private bool isImageLoaded;
        private BitmapImage _loading;

#if DEBUG
        ~ImageViewModelBase()
        {
            Debug.Assert(_loading == null);
            Debug.Assert(Image == null);
        }
#endif

        public abstract void LoadImage(int displayWidth = 100);

        protected void LoadImage(Uri uri, int displayWidth)
        {
            if (isImageRequested) return;
            isImageRequested = true;

            _loading = new BitmapImage() { DecodePixelWidth = displayWidth };
            _loading.ImageOpened += ImageLoaded;
            _loading.ImageFailed += ImageFailed;
            _loading.CreateOptions = BitmapCreateOptions.BackgroundCreation;
            _loading.UriSource = uri;
        }

        private void ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            isImageLoaded = true;

            _loading.ImageOpened -= ImageLoaded;
            _loading.ImageFailed -= ImageFailed;

            if (!isImageRequested)
            {
                DisposeImage(_loading);
                _loading = null;
            }
        }

        private void ImageLoaded(object sender, RoutedEventArgs e)
        {
            isImageLoaded = true;

            _loading.ImageOpened -= ImageLoaded;
            _loading.ImageFailed -= ImageFailed;

            if (isImageRequested)
            {
                Image = _loading;
            }
            else
            {
                DisposeImage(_loading);
                _loading = null;
            }
        }

        public virtual void UnloadImage()
        {
            isImageRequested = false;

            if (isImageLoaded)
            {
                isImageLoaded = false;

                DisposeImage(Image);
                Image = null;
                _loading = null;
            }
        }

        private void DisposeImage(BitmapImage bi)
        {
            try
            {
                using (var ms = new MemoryStream(new byte[] { 0x0 }))
                {
                    bi.SetSource(ms);
                }
            }
            catch
            {
            }
            bi.UriSource = null;
            Image = null;
        }
    }
}
