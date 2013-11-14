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
            (d as MultiResolutionImage).DownloadProgressCommandChanged();
        }

        #endregion

        private Size? _size;
        private Image _image;

        private bool _isShowingFullSize, _isLoadingFullSize;
        private bool _isShowingThumbnail, _isLoadingThumbnail;
        private BitmapImage _thumbnail, _fullsize;
        private CancellationTokenSource _thumbCancel, _fullCancel;

        private static int x = 0;

        private int y = 0;
        public MultiResolutionImage()
        {
            DefaultStyleKey = typeof(MultiResolutionImage);
            y = x++;

            Loaded += MultiResolutionImage_Loaded;
            Unloaded += MultiResolutionImage_Unloaded;
        }

        private void MultiResolutionImage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadThumbnailIfNeeded();
            LoadFullSizeIfNeeded();
        }

        private void MultiResolutionImage_Unloaded(object sender, RoutedEventArgs e)
        {
            CancelLoadingThumbnail();
            CancelLoadingFullSize();
            UnloadThumbnail();
            UnloadFullSize();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _image = (Image)GetTemplateChild("ImageContainer");

            if (DesignerProperties.GetIsInDesignMode(this))
            {
                // We have to load resources special in design mode, we can't read
                // from a stream. To prevent other functions overwriting the image,
                // new up the private member afterwards.
                _image.Source = new BitmapImage(Thumbnail);
                _image = new Image();
                return;
            }

            LoadThumbnailIfNeeded();
            LoadFullSizeIfNeeded();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return availableSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _size = finalSize;
            LoadThumbnailIfNeeded();
            LoadFullSizeIfNeeded();
            UpdateFullSizeDecodeSize();
            
            return base.ArrangeOverride(finalSize);
        }

        private void ThumbnailChanged()
        {
            CancelLoadingThumbnail();
            UnloadThumbnail();
            LoadThumbnailIfNeeded();
            LoadFullSizeIfNeeded();
        }

        private void FullSizeChanged()
        {
            CancelLoadingFullSize();
            UnloadFullSize();
            LoadThumbnailIfNeeded();
            LoadFullSizeIfNeeded();
        }

        private void DownloadProgressCommandChanged()
        {
            throw new NotImplementedException();
        }

        private void LoadThumbnailIfNeeded()
        {
            if (!_isShowingThumbnail && !_isShowingFullSize && !_isLoadingThumbnail && Thumbnail != null && _size != null)
            {
                LoadThumbnailImage().ContinueWith(result =>
                {
                    throw result.Exception;
                }, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
            }
        }

        private void LoadFullSizeIfNeeded()
        {
            if (!_isShowingFullSize && !_isLoadingFullSize && _size != null && _isShowingThumbnail && FullSize != null &&
                (_thumbnail.PixelWidth < _size.Value.Width || _thumbnail.PixelHeight < _size.Value.Height))
            {
                LoadFullSizeImage().ContinueWith(result =>
                {
                    throw result.Exception;
                }, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
            }
        }

        private async Task LoadThumbnailImage()
        {
            Debug.Assert(_thumbnail == null);
            Debug.Assert(_thumbCancel == null);
            Debug.Assert(_isLoadingThumbnail == false);
            Debug.Assert(_isShowingThumbnail == false);

            _isLoadingThumbnail = true;
            _thumbCancel = new CancellationTokenSource();
            CancellationToken ct = _thumbCancel.Token;

            Uri uri = Thumbnail;

            Stream stream;
            if (Thumbnail.Scheme == "file")
            {
                try
                {
                    stream = await LoadLocalFile(Thumbnail, ct);
                }
                catch (TaskCanceledException)
                {
                    return;
                }
            }
            else
            {
                try
                {
                    stream = await LoadRemoteFile(Thumbnail, ct);
                }
                catch (TaskCanceledException)
                {
                    return;
                }
            }

            if (ct.IsCancellationRequested)
            {
                return;
            }

            _thumbnail = new BitmapImage()
            {
                CreateOptions = BitmapCreateOptions.DelayCreation
            };
            try
            {
                _thumbnail.SetSource(stream);
            }
            catch (Exception ex)
            {
                if (ex.HResult == -2146233088)
                {
                    // Image unrecognized
                    _isLoadingThumbnail = false;
                    _thumbCancel = null;
                    _thumbnail.UriSource = null;
                    _thumbnail = null;
                    return;
                }
                else
                {
                    throw;
                }
            }

            _image.Source = _thumbnail;

            _isLoadingThumbnail = false;
            _isShowingFullSize = false;
            _isShowingThumbnail = true;

            LoadFullSizeIfNeeded();
        }

        private async Task<Stream> LoadRemoteFile(Uri uri, CancellationToken token)
        {
            HttpResponseMessage response;
            response = await new HttpClient().GetAsync(uri, HttpCompletionOption.ResponseContentRead, token);
            return await response.Content.ReadAsStreamAsync();
        }

        private async Task<Stream> LoadLocalFile(Uri uri, CancellationToken token)
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(uri.LocalPath);
            return await file.OpenStreamForReadAsync();
        }

        private async Task LoadFullSizeImage()
        {
            Debug.Assert(_fullsize == null);
            Debug.Assert(_fullCancel == null);
            Debug.Assert(_isLoadingFullSize == false);
            Debug.Assert(_isShowingFullSize == false);

            _isLoadingFullSize = true;
            _fullCancel = new CancellationTokenSource();
            CancellationToken ct = _thumbCancel.Token;

            Stream stream;
            if (FullSize.Scheme == "file")
            {
                try
                {
                    stream = await LoadLocalFile(FullSize, ct);
                }
                catch (TaskCanceledException)
                {
                    return;
                }
            }
            else
            {
                try
                {
                    stream = await LoadRemoteFile(FullSize, ct);
                }
                catch (TaskCanceledException)
                {
                    return;
                }
            }

            if (ct.IsCancellationRequested)
            {
                return;
            }

            Debug.Assert(_fullsize == null);
            _fullsize = new BitmapImage()
            {
                CreateOptions = BitmapCreateOptions.DelayCreation,
                DecodePixelType = DecodePixelType.Logical,
                DecodePixelWidth = (int)_size.Value.Width
            };
            try
            {
                _fullsize.SetSource(stream);
            }
            catch (Exception ex)
            {
                if (ex.HResult == -2146233088)
                {
                    // Image unrecognized
                    _isLoadingFullSize = false;
                    _fullCancel = null;
                    _fullsize.UriSource = null;
                    _fullsize = null;
                    return;
                }
                else
                {
                    throw;
                }
            }

            _image.Source = _fullsize;

            _isLoadingFullSize = false;
            _isShowingThumbnail = false;
            _isShowingFullSize = true;
        }

        private void UpdateFullSizeDecodeSize()
        {
            if (_isShowingFullSize)
            {
                _fullsize.DecodePixelWidth = (int)_size.Value.Width;
            }
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
                _thumbnail.UriSource = null;
                _thumbnail = null;
                _thumbCancel = null;
                if (_isShowingThumbnail)
                {
                    _image.Source = null;
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
                _fullsize.UriSource = null;
                _fullsize = null;
                _fullCancel = null;
                if (_isShowingFullSize)
                {
                    _image.Source = null;
                    _isShowingFullSize = false;
                }
            }
        }

        private void DisplayThumbnail()
        {
            Debug.Assert(!_isLoadingThumbnail);
            Debug.Assert(_thumbnail != null);

            _image.Source = _thumbnail;

            _isShowingFullSize = false;
            _isShowingThumbnail = true;
        }

        private void DisplayFullSize()
        {
            Debug.Assert(!_isLoadingFullSize);
            Debug.Assert(_fullsize != null);

            _image.Source = _fullsize;

            _isShowingFullSize = true;
            _isShowingThumbnail = false;
        }
    }
}