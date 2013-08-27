/* 
    Copyright (c) 2012 Microsoft Corporation.  All rights reserved.
    Use of this sample source code is subject to the terms of the Microsoft license 
    agreement under which you licensed this sample source code and is provided AS-IS.
    If you did not accept the terms of the license agreement, you are not authorized 
    to use this sample source code.  For the terms of the license, please see the 
    license agreement between you and Microsoft.
  
    To see all Code Samples for Windows Phone, visit http://go.microsoft.com/fwlink/?LinkID=219604 
  
*/
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Microsoft.Phone.Controls
{
    /// <summary>
    /// Knowns how to display an IThumbnailedImage, picking the thumbnail or 
    /// full resolution image to display based on the container size.  The
    /// IThumbnailedImage to display should be assigned to the DataContext
    /// property.
    /// </summary>
    public class ThumbnailedImageViewer : Control, INotifyPropertyChanged
    {
        private enum ImageBindingState { ScreenSizeThumbnail, FullSizePhoto }
        private ImageBindingState _imageBindingState = ImageBindingState.ScreenSizeThumbnail;
        private Image _image = null;
        private GIFViewer _gif = null;
        private FrameworkElement _placeholder = null;

        private BitmapImage _thumbnailBitmapImage = null;
        private ImageSource _thumbnailImageSource = null;
        private BitmapImage _fullResolutionBitmapImage = null;
        private ImageSource _fullResolutionImageSource = null;

        private int _progress;
        public int Progress
        {
            get
            {
                return _progress;
            }
            set
            {
                _progress = value;
                NotifyPropertyChanged();
            }
        }

        public ThumbnailedImageViewer()
        {
            DefaultStyleKey = typeof(ThumbnailedImageViewer);

            // Register for DataContext change notifications
            //
            DependencyProperty dataContextDependencyProperty = System.Windows.DependencyProperty.RegisterAttached("DataContextProperty", typeof(object), typeof(FrameworkElement), new System.Windows.PropertyMetadata(OnDataContextPropertyChanged));
            SetBinding(dataContextDependencyProperty, new Binding());
        }

        public void ReleaseImages()
        {
            if (_thumbnailBitmapImage != null)
            {
                _thumbnailBitmapImage.DownloadProgress -= OnDownloadProgress;
                _thumbnailBitmapImage.ImageOpened -= OnThumbnailOpened;
                try
                {
                    using (var ms = new MemoryStream(new byte[] { 0x0 }))
                    {
                        _thumbnailBitmapImage.SetSource(ms);
                    }
                }
                catch
                {
                }
                _thumbnailBitmapImage.UriSource = null;
            }
            if (_fullResolutionBitmapImage != null)
            {
                _fullResolutionBitmapImage.DownloadProgress -= OnDownloadProgress;
                _fullResolutionBitmapImage.ImageOpened -= OnFullSizeImageOpened;
                try
                {
                    using (var ms = new MemoryStream(new byte[] { 0x0 }))
                    {
                        _fullResolutionBitmapImage.SetSource(ms);
                    }
                }
                catch
                {
                }
                _fullResolutionBitmapImage.UriSource = null;
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (DataContext is IThumbnailedImage)
            {
                if ((_imageBindingState == ImageBindingState.ScreenSizeThumbnail) &&
                    (_image.Visibility == System.Windows.Visibility.Visible) &&      // make sure the image is loaded before measuring its size
                    (CurrentImageSizeIsTooSmall(availableSize)))
                {
                    BeginLoadingFullResolutionImage();
                }
            }

            return base.MeasureOverride(availableSize);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _image = GetTemplateChild("Image") as Image;
            _gif = GetTemplateChild("GIFImage") as GIFViewer;
            _placeholder = GetTemplateChild("Placeholder") as FrameworkElement;

            ShowPlaceholder();
        }

        private bool isLoaded = false;
        private static void OnDataContextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ThumbnailedImageViewer mediaItemViewer = (ThumbnailedImageViewer)d;

            IThumbnailedImage newPhoto = e.NewValue as IThumbnailedImage;

            mediaItemViewer.Unload();
            mediaItemViewer.ClearImageSources();
            mediaItemViewer.Reset();

            if (e.NewValue != null)
            {
                mediaItemViewer.ShowPlaceholder();
                mediaItemViewer.BeginLoadingThumbnail();
            }
            else
            {
                mediaItemViewer._image.Source = null;
            }
        }

        private void Unload()
        {
            if (_fullResolutionBitmapImage != null)
            {
                _fullResolutionBitmapImage.DownloadProgress -= OnDownloadProgress;
            }
        }

        private void Reset()
        {
            lock (animateLock)
            {
                isLoaded = false;
            }
        }

        private void OnThumbnailOpened(object sender, EventArgs e)
        {
            _image.Source = _thumbnailImageSource;

            ClearImageSources();

            ShowImage();
            InvalidateMeasure();
        }

        private void OnFullSizeImageOpened(object sender, EventArgs e)
        {
            lock (animateLock)
            {
                isLoaded = true;
                if (animate && ((IThumbnailedImage)DataContext).IsGIF)
                {
                    _gif.Display();
                }
                else
                {
                    _image.Source = _fullResolutionImageSource;
                }

                Unload();
                ClearImageSources();

                if (animate && ((IThumbnailedImage)DataContext).IsGIF)
                {
                    ShowAnimation();
                }
                else
                {
                    ShowImage();
                }
                InvalidateMeasure();
            }
        }

        private void OnDownloadProgress(object sender, DownloadProgressEventArgs e)
        {
            Progress = e.Progress;
        }

        private void ClearImageSources()
        {
            if (_thumbnailBitmapImage != null)
            {
                _thumbnailBitmapImage.ImageOpened -= OnThumbnailOpened;
            }
            if (_fullResolutionBitmapImage != null)
            {
                _fullResolutionBitmapImage.ImageOpened -= OnFullSizeImageOpened;
            }
        }

        private bool CurrentImageSizeIsTooSmall(Size availableSize)
        {
            if (_image.Source == null)
            {
                return true;
            }

            bool toReturn = ((((BitmapImage)_image.Source).PixelWidth < availableSize.Width) && (((BitmapImage)_image.Source).PixelHeight < availableSize.Height));

            if (toReturn)
            {
                //Tracing.Trace("MediaItemViewer.CurrentImageSizeIsTooSmall() - switching from thumbnail to full res photo because the thumbnail is too small (" + source.PixelWidth + ", " + source.PixelHeight + ") for the available size (" + availableSize + ")");
            }

            return toReturn;
        }

        private void BeginLoadingThumbnail()
        {
            if (DataContext is IThumbnailedImage == false)
            {
                return;
            }

            if (_thumbnailBitmapImage != null)
            {
                try
                {
                    using (var ms = new MemoryStream(new byte[] { 0x0 }))
                    {
                        _thumbnailBitmapImage.SetSource(ms);
                    }
                }
                catch
                {
                }
                _thumbnailBitmapImage.UriSource = null;
                _thumbnailBitmapImage.ImageOpened -= OnThumbnailOpened;
            }
            _thumbnailBitmapImage = null;

            _thumbnailBitmapImage = new BitmapImage();
            _thumbnailBitmapImage.ImageOpened += OnThumbnailOpened;
            _thumbnailBitmapImage.CreateOptions = BitmapCreateOptions.BackgroundCreation;
            _thumbnailBitmapImage.UriSource = ((IThumbnailedImage)DataContext).ThumbnailSrc;
            _thumbnailImageSource = _thumbnailBitmapImage;

            _imageBindingState = ImageBindingState.ScreenSizeThumbnail;
        }

        private void BeginLoadingFullResolutionImage()
        {
            if (DataContext is IThumbnailedImage == false)
            {
                return;
            }

            if (_fullResolutionBitmapImage != null)
            {
                try
                {
                    using (var ms = new MemoryStream(new byte[] { 0x0 }))
                    {
                        _fullResolutionBitmapImage.SetSource(ms);
                    }
                }
                catch
                {
                }
                _fullResolutionBitmapImage.UriSource = null;
                _fullResolutionBitmapImage.ImageOpened -= OnFullSizeImageOpened;
            }
            _fullResolutionBitmapImage = null;

            _fullResolutionBitmapImage = new BitmapImage();
            _fullResolutionBitmapImage.ImageOpened += OnFullSizeImageOpened;
            _fullResolutionBitmapImage.CreateOptions = BitmapCreateOptions.BackgroundCreation;
            _fullResolutionBitmapImage.DownloadProgress += OnDownloadProgress;
            _fullResolutionBitmapImage.UriSource = ((IThumbnailedImage)DataContext).ImageSrc;
            _fullResolutionImageSource = _fullResolutionBitmapImage;

            _imageBindingState = ImageBindingState.FullSizePhoto;

            Progress = 0;
        }

        private bool animate = false;
        private object animateLock = new object();
        public void Animate()
        {
            if (DataContext is IThumbnailedImage == false)
            {
                return;
            }

            lock (animateLock)
            {
                if (!animate && isLoaded && ((IThumbnailedImage)DataContext).IsGIF)
                {
                    _gif.Display();
                    ShowAnimation();
                }
                animate = true;
            }
        }

        public void Unanimate()
        {
            if (DataContext is IThumbnailedImage == false)
            {
                return;
            }

            lock (animateLock)
            {
                if (animate && isLoaded && ((IThumbnailedImage)DataContext).IsGIF)
                {
                    _gif.Unload();
                    ShowImage();
                }
                animate = false;
            }
        }

        private void ShowPlaceholder()
        {
            if (_placeholder != null)
            {
                _placeholder.Visibility = System.Windows.Visibility.Visible;
            }
            if (_image != null)
            {
                _image.Visibility = System.Windows.Visibility.Collapsed;
            }
            if (_gif != null)
            {
                _gif.Collapse();
            }
        }

        private void ShowImage()
        {
            if (_image != null)
            {
                _image.Visibility = System.Windows.Visibility.Visible;
            }
            if (_placeholder != null)
            {
                _placeholder.Visibility = System.Windows.Visibility.Collapsed;
            }
            if (_gif != null)
            {
                _gif.Collapse();
            }
        }
        private void ShowAnimation()
        {
            if (_placeholder != null)
            {
                _placeholder.Visibility = System.Windows.Visibility.Collapsed;
            }
            if (_gif != null)
            {
                _image.Opacity = 0.99;
                _gif.Show(() =>
                {
                    _image.Visibility = System.Windows.Visibility.Collapsed;
                    _image.Opacity = 1.0;
                });
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
