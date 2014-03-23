using _4charm.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Windows.Storage;

namespace _4charm.Controls.Image
{
    public class MultiResolutionImage : Control
    {
        #region ThumbnailURI DependencyProperty

        public static readonly DependencyProperty ThumbnailURIProperty = DependencyProperty.Register(
            "ThumbnailURI",
            typeof(Uri),
            typeof(MultiResolutionImage),
            new PropertyMetadata(null, OnThumbnailURIChanged));

        public Uri ThumbnailURI
        {
            get { return (Uri)GetValue(ThumbnailURIProperty); }
            set { SetValue(ThumbnailURIProperty, value); }
        }

        private static void OnThumbnailURIChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as MultiResolutionImage).ThumbnailURIChanged();
        }

        #endregion

        #region FullSizeURI DependencyProperty

        public static readonly DependencyProperty FullSizeURIProperty = DependencyProperty.Register(
            "FullSizeURI",
            typeof(Uri),
            typeof(MultiResolutionImage),
            new PropertyMetadata(null, OnFullSizeURIChanged));

        public Uri FullSizeURI
        {
            get { return (Uri)GetValue(FullSizeURIProperty); }
            set { SetValue(FullSizeURIProperty, value); }
        }

        private static void OnFullSizeURIChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as MultiResolutionImage).FullSizeURIChanged();
        }

        #endregion

        #region AspectRatio DependencyProperty

        public static readonly DependencyProperty AspectRatioProperty = DependencyProperty.Register(
            "AspectRatio",
            typeof(double),
            typeof(MultiResolutionImage),
            new PropertyMetadata(-1.0, OnAspectRationChanged));

        public double AspectRatio
        {
            get { return (double)GetValue(AspectRatioProperty); }
            set { SetValue(AspectRatioProperty, value); }
        }

        private static void OnAspectRationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as MultiResolutionImage).AspectRatioChanged();
        }

        #endregion

        #region DownloadProgressCommand DependencyProperty

        public static readonly DependencyProperty DownloadProgressCommandProperty = DependencyProperty.Register(
            "DownloadProgressCommand",
            typeof(ICommand),
            typeof(MultiResolutionImage),
            new PropertyMetadata(null, OnDownloadProgressCommandChanged));

        public ICommand DownloadProgressCommand
        {
            get { return (ICommand)GetValue(DownloadProgressCommandProperty); }
            set { SetValue(DownloadProgressCommandProperty, value); }
        }

        private static void OnDownloadProgressCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }

        #endregion

        private Size? _size;
        private IPreloadedImage _image;

        private bool _isShowingFullSize, _isShowingThumbnail;
        private bool _isLoadingFullSize, _isLoadingThumbnail;
        private CancellationTokenSource _thumbCancel, _fullCancel;
        private Stream _fullsize, _thumbnail;

        public MultiResolutionImage()
        {
            DefaultStyleKey = typeof(MultiResolutionImage);

            SizeChanged += MultiResolutionImage_SizeChanged;
            Loaded += MultiResolutionImage_Loaded;
            Unloaded += MultiResolutionImage_Unloaded;
        }

        private void MultiResolutionImage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadAppropriateImageIfNeeded();
        }

        private void MultiResolutionImage_Unloaded(object sender, RoutedEventArgs e)
        {
            CancelThumbnail();
            CancelFullSize();
            UnloadThumbnail();
            UnloadFullSize();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (AspectRatio == -1.0)
            {
                return availableSize;
            }

            double availableAspect = availableSize.Width / (double)availableSize.Height;
            if (AspectRatio >= availableAspect)
            {
                return new Size(availableSize.Width, availableSize.Width / AspectRatio);
            }
            else
            {
                return new Size(availableSize.Height * AspectRatio, availableSize.Height);
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _image = (IPreloadedImage)GetTemplateChild("PreloadedImageContainer");

            LoadAppropriateImageIfNeeded();
        }

        private void MultiResolutionImage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _size = e.NewSize;

            if (e.NewSize != e.PreviousSize)
            {
                LoadAppropriateImageIfNeeded();
            }
        }

        private void AspectRatioChanged()
        {
            InvalidateMeasure();
        }

        private void ThumbnailURIChanged()
        {
            UnloadThumbnail();
            CancelThumbnail();
            UnloadFullSize();
            CancelFullSize();

            LoadAppropriateImageIfNeeded();
        }

        private void FullSizeURIChanged()
        {
            UnloadFullSize();
            if (_fullCancel != null)
            {
                _isLoadingFullSize = false;
                _fullCancel.Cancel();
                _fullCancel = null;
            }

            LoadAppropriateImageIfNeeded();
        }

        private void LoadAppropriateImageIfNeeded()
        {
            if (_isShowingFullSize || _size == null)
            {
                // If we are showing the full size already, there is nothing to do.
                return;
            }

            if (!_isShowingThumbnail && ThumbnailURI != null)
            {
                if (!_isLoadingThumbnail)
                {
                    LoadThumbnailImage();
                }
            }
            else if (_isShowingThumbnail && FullSizeURI != null)
            {
                if (!_isLoadingFullSize)
                {
                    LoadFullSizeImage();
                }
            }
        }

        private void LoadThumbnailImage()
        {
            Debug.Assert(_thumbnail == null);
            Debug.Assert(!_isShowingThumbnail);
            Debug.Assert(!_isShowingFullSize);
            Debug.Assert(!_isLoadingFullSize);
            Debug.Assert(!_isLoadingThumbnail);
            Debug.Assert(_thumbCancel == null);
            Debug.Assert(_fullCancel == null);

            _thumbCancel = new CancellationTokenSource();
            CancellationToken token = _thumbCancel.Token;

            _isLoadingThumbnail = true;
            GetURIStream(ThumbnailURI).ContinueWith(task =>
            {
                if (!token.IsCancellationRequested)
                {
                    Debug.Assert(_thumbCancel.Token == token);
                    _thumbCancel = null;
                    _isLoadingThumbnail = false;
                }

                if (task.IsFaulted || token.IsCancellationRequested)
                {
                    return;
                }

                UnloadThumbnail();
                UnloadFullSize();
                _image.SetStreamSource(task.Result, Path.GetExtension(ThumbnailURI.AbsolutePath));

                _thumbnail = task.Result;
                _isShowingThumbnail = true;
                _isShowingFullSize = false;

                if (FullSizeURI != null)
                {
                    LoadFullSizeImage();
                }
            }, TaskContinuationOptions.ExecuteSynchronously);
        }

        private void LoadFullSizeImage()
        {
            Debug.Assert(_fullsize == null);
            Debug.Assert(!_isShowingFullSize);
            Debug.Assert(_isShowingThumbnail);
            Debug.Assert(!_isLoadingFullSize);
            Debug.Assert(!_isLoadingThumbnail);
            Debug.Assert(_thumbCancel == null);
            Debug.Assert(_fullCancel == null);

            _fullCancel = new CancellationTokenSource();
            CancellationToken token = _fullCancel.Token;

            _isLoadingFullSize = true;
            GetURIStream(FullSizeURI, progress =>
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                if (DownloadProgressCommand != null)
                {
                    DownloadProgressCommand.Execute(progress);
                }
            }).ContinueWith(task =>
            {
                if (!token.IsCancellationRequested)
                {
                    Debug.Assert(_fullCancel.Token == token);
                    _fullCancel = null;
                    _isLoadingFullSize = false;
                }

                if (task.IsFaulted || token.IsCancellationRequested)
                {
                    return;
                }

                UnloadFullSize();
                _image.SetStreamSource(task.Result, Path.GetExtension(FullSizeURI.AbsolutePath));

                _fullsize = task.Result;
                _isShowingThumbnail = false;
                _isShowingFullSize = true;
            }, TaskContinuationOptions.ExecuteSynchronously);
        }

        private async Task<Stream> GetURIStream(Uri uri, Action<int> progress = null)
        {
            if (uri.Scheme == "file")
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(uri.LocalPath);
                return await file.OpenStreamForReadAsync();
            }
            else
            {
                return new MemoryStream(await RequestManager.Current.GetByteArrayWithProgressAsync(uri, progress));
            }
        }

        private void CancelThumbnail()
        {
            if (_isLoadingThumbnail)
            {
                _isLoadingThumbnail = false;
                _thumbCancel.Cancel();
                _thumbCancel = null;
            }
        }

        private void CancelFullSize()
        {
            if (_isLoadingFullSize)
            {
                _isLoadingFullSize = false;
                _fullCancel.Cancel();
                _fullCancel = null;
            }
        }

        private void UnloadThumbnail()
        {
            if (_thumbnail != null)
            {
                _thumbnail = null;
                if (_isShowingThumbnail)
                {
                    _image.UnloadStreamSource();
                    _isShowingThumbnail = false;
                }
            }
        }

        private void UnloadFullSize()
        {
            if (_fullsize != null)
            {
                _fullsize = null;
                if (_isShowingFullSize)
                {
                    _image.UnloadStreamSource();
                    _isShowingFullSize = false;
                }
            }
        }
    }
}