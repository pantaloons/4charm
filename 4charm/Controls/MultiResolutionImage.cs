using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using _4charm.Models;
using System.Windows.Media.Imaging;
using Windows.Storage;

namespace _4charm.Controls
{
    public class MultiResolutionImage : Control
    {
        #region Thumbnail DependencyProperty

        public static readonly DependencyProperty ThumbnailProperty = DependencyProperty.Register(
            "Thumbnail",
            typeof(Uri),
            typeof(MultiResolutionImage),
            new PropertyMetadata(null, OnThumbnailChanged));

        public Uri Thumbnail
        {
            get { return (Uri)GetValue(ThumbnailProperty); }
            set { SetValue(ThumbnailProperty, value); }
        }

        private static void OnThumbnailChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as MultiResolutionImage).ThumbnailChanged();
        }

        #endregion

        #region FullSize DependencyProperty

        public static readonly DependencyProperty FullSizeProperty = DependencyProperty.Register(
            "FullSize",
            typeof(Uri),
            typeof(MultiResolutionImage),
            new PropertyMetadata(null, OnFullSizeChanged));

        public Uri FullSize
        {
            get { return (Uri)GetValue(FullSizeProperty); }
            set { SetValue(FullSizeProperty, value); }
        }

        private static void OnFullSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as MultiResolutionImage).FullSizeChanged();
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

        private bool _isLoaded;
        private Size? _size;
        private IPreloadedImage _image;

        private bool _isShowingFullSize, _isLoadingFullSize;
        private bool _isShowingThumbnail, _isLoadingThumbnail;
        private Stream _thumbnail, _fullsize;
        private CancellationTokenSource _thumbCancel, _fullCancel;

        public MultiResolutionImage()
        {
            DefaultStyleKey = typeof(MultiResolutionImage);

            Loaded += MultiResolutionImage_Loaded;
            Unloaded += MultiResolutionImage_Unloaded;
            SizeChanged += MultiResolutionImage_SizeChanged;
        }

        private void MultiResolutionImage_Loaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
            LoadAppropriateImageIfNeeded();
        }

        private void MultiResolutionImage_Unloaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = false;
            CancelLoadingThumbnail();
            CancelLoadingFullSize();
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

            if (DesignerProperties.GetIsInDesignMode(this))
            {
                _isShowingFullSize = true;
                ((StaticImage)_image).SetUriSource(Thumbnail);
                _image = new StaticImage();
                return;
            }

            LoadAppropriateImageIfNeeded();
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _size = finalSize;

            return base.ArrangeOverride(finalSize);
        }

        private void MultiResolutionImage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize != e.PreviousSize)
            {
                LoadAppropriateImageIfNeeded();
            }   
        }

        private void AspectRatioChanged()
        {
            InvalidateMeasure();
        }

        private void ThumbnailChanged()
        {
            CancelLoadingThumbnail();
            CancelLoadingFullSize();
            UnloadThumbnail();
            UnloadFullSize();
            LoadAppropriateImageIfNeeded();
        }

        private void FullSizeChanged()
        {
            CancelLoadingFullSize();
            UnloadFullSize();
            LoadAppropriateImageIfNeeded();
        }

        private void LoadAppropriateImageIfNeeded()
        {
            if (_isShowingFullSize || _isLoadingThumbnail || _isLoadingFullSize || !_isLoaded || _size == null)
            {
                return;
            }

            if (!_isShowingThumbnail && Thumbnail != null)
            {
                LoadThumbnailImage();
            }
            else if (_isShowingThumbnail && FullSize != null)
            {
                LoadFullSizeImage();
            }
        }

        private void LoadThumbnailImage()
        {
            Debug.Assert(_isLoaded);
            Debug.Assert(_thumbCancel == null);
            Debug.Assert(_fullCancel == null);
            Debug.Assert(!_isLoadingThumbnail);
            Debug.Assert(!_isLoadingFullSize);

            _isLoadingThumbnail = true;
            _thumbCancel = new CancellationTokenSource();
            LoadThumbnailImageAsync(_thumbCancel.Token).ContinueWith(result =>
            {
                _isLoadingThumbnail = false;
                _thumbCancel = null;

                if (_isShowingThumbnail && FullSize != null)
                {
                    LoadFullSizeImage();
                }
            }, TaskContinuationOptions.ExecuteSynchronously);
        }

        private void LoadFullSizeImage()
        {
            Debug.Assert(_isLoaded);
            Debug.Assert(_thumbCancel == null);
            Debug.Assert(_fullCancel == null);
            Debug.Assert(!_isLoadingThumbnail);
            Debug.Assert(!_isLoadingFullSize);

            _isLoadingFullSize = true;
            _fullCancel = new CancellationTokenSource();
            LoadFullSizeImageAsync(_fullCancel.Token).ContinueWith(result =>
            {
                _isLoadingFullSize = false;
                _fullCancel = null;
            }, TaskContinuationOptions.ExecuteSynchronously);
        }

        private async Task LoadThumbnailImageAsync(CancellationToken token)
        {
            Debug.Assert(_isLoaded);
            Debug.Assert(_thumbnail == null);
            Debug.Assert(_isShowingThumbnail == false);
            Debug.Assert(_isLoadingFullSize == false);
            Debug.Assert(_fullCancel == null);
            Debug.Assert(_isShowingFullSize == false);
            
            Stream stream;
            try
            {
                stream = await GetUriStream(Thumbnail, progress => { }, token);
            }
            catch
            {
                return;
            }

            if (token.IsCancellationRequested)
            {
                return;
            }

            bool success = await _image.SetStreamSource(stream, Path.GetExtension(Thumbnail.AbsolutePath), token);

            if (success)
            {
                _thumbnail = stream;
                _isShowingThumbnail = true;
            }
        }

        private async Task LoadFullSizeImageAsync(CancellationToken token)
        {
            Debug.Assert(_isLoaded);
            Debug.Assert(_fullsize == null);
            Debug.Assert(_isShowingFullSize == false);
            Debug.Assert(_isLoadingThumbnail == false);
            Debug.Assert(_thumbCancel == null);
            Debug.Assert(_isShowingThumbnail == true);

            Stream stream;
            try
            {
                stream = await GetUriStream(FullSize, progress =>
                {
                    if (DownloadProgressCommand != null)
                    {
                        DownloadProgressCommand.Execute(progress);
                    }
                }, token);
            }
            catch
            {
                return;
            }

            if (token.IsCancellationRequested)
            {
                return;
            }

            bool success = await _image.SetStreamSource(stream, Path.GetExtension(FullSize.AbsolutePath), token);

            if (success)
            {
                _fullsize = stream;
                _isShowingThumbnail = false;
                _isShowingFullSize = true;
            }
        }

        private async Task<Stream> GetUriStream(Uri uri, Action<int> progress, CancellationToken token)
        {
            Stream stream;
            if (uri.Scheme == "file")
            {
                stream = await LoadLocalFile(uri, token);
            }
            else
            {
                stream = await LoadRemoteFile(uri, progress, token);
            }
            return stream;
        }

        private async Task<Stream> LoadRemoteFile(Uri uri, Action<int> progress, CancellationToken token)
        {
#if DEBUG
            // In debug mode the cancellations spew too much junk into the debugger so we have to disable them.
            token = CancellationToken.None;
#endif
            byte[] response = await RequestManager.Current.GetByteArrayWithProgressAsync(uri, progress, token);
            return new MemoryStream(response);
        }

        private async Task<Stream> LoadLocalFile(Uri uri, CancellationToken token)
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(uri.LocalPath);
            return await file.OpenStreamForReadAsync();
        }

        private void CancelLoadingThumbnail()
        {
            if (_isLoadingThumbnail)
            {
                _thumbCancel.Cancel();
                _thumbCancel = null;
                _isLoadingThumbnail = false;
            }
        }

        private void UnloadThumbnail()
        {
            if (_thumbnail != null)
            {
                _thumbnail = null;
                _thumbCancel = null;
                if (_isShowingThumbnail)
                {
                    _image.UnloadStreamSource();
                    _isShowingThumbnail = false;
                }
            }
        }

        private void CancelLoadingFullSize()
        {
            if (_isLoadingFullSize)
            {
                _fullCancel.Cancel();
                _fullCancel = null;
                _isLoadingFullSize = false;
            }
        }

        private void UnloadFullSize()
        {
            if (_fullsize != null)
            {
                _fullsize = null;
                _fullCancel = null;
                if (_isShowingFullSize)
                {
                    _image.UnloadStreamSource();
                    _isShowingFullSize = false;
                }
            }
        }
    }
}