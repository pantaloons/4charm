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
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _image = (IPreloadedImage)GetTemplateChild("PreloadedImageContainer");

            if (DesignerProperties.GetIsInDesignMode(this))
            {
                // We have to load resources special in design mode, we can't read
                // from a stream. To prevent other functions overwriting the image,
                // new up the private member afterwards.

                //(_image as StaticImage)._container.Source = new BitmapImage(Thumbnail);
                //(_image as StaticImage)._container = new Image();
                //if (rootImage == null) throw new Exception("qqq");
                //rootImage.Source = 
                //_image = new StaticImage();
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
                (_image.PixelWidth < _size.Value.Width || _image.PixelHeight < _size.Value.Height))
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

            try
            {
                _thumbnail = await LoadImage(Thumbnail, _thumbCancel.Token);
            }
            catch (Exception ex)
            {
                if (ex.HResult == -2146233088)
                {
                    // Image unrecognized
                    _isLoadingThumbnail = false;
                    _thumbCancel = null;

                    return;
                }
                else
                {
                    throw;
                }
            }

            _isLoadingThumbnail = false;
            _isShowingFullSize = false;
            _isShowingThumbnail = true;

            LoadFullSizeIfNeeded();
        }

        private async Task LoadFullSizeImage()
        {
            Debug.Assert(_fullsize == null);
            Debug.Assert(_fullCancel == null);
            Debug.Assert(_isLoadingFullSize == false);
            Debug.Assert(_isShowingFullSize == false);

            _isLoadingFullSize = true;
            _fullCancel = new CancellationTokenSource();

            try
            {
                _fullsize = await LoadImage(FullSize, _thumbCancel.Token);
            }
            catch (Exception ex)
            {
                if (ex.HResult == -2146233088)
                {
                    // Image unrecognized
                    _isLoadingFullSize = false;
                    _fullCancel = null;
                    return;
                }
                else
                {
                    throw;
                }
            }

            _isLoadingFullSize = false;
            _isShowingThumbnail = false;
            _isShowingFullSize = true;
        }

        private async Task<Stream> LoadImage(Uri uri, CancellationToken token)
        {
            Stream stream;
            if (uri.Scheme == "file")
            {
                try
                {
                    stream = await LoadLocalFile(uri, token);
                }
                catch (TaskCanceledException)
                {
                    return null;
                }
            }
            else
            {
                try
                {
                    stream = await LoadRemoteFile(uri, token);
                }
                catch (TaskCanceledException)
                {
                    return null;
                }
            }

            if (token.IsCancellationRequested)
            {
                return null;
            }

            _image.SetStreamSource(stream, Path.GetExtension(uri.AbsolutePath));
            return stream;
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

        private void DisplayThumbnail()
        {
            Debug.Assert(!_isLoadingThumbnail);
            Debug.Assert(_thumbnail != null);

            _image.SetStreamSource(_thumbnail, Path.GetExtension(Thumbnail.AbsolutePath));

            _isShowingFullSize = false;
            _isShowingThumbnail = true;
        }

        private void DisplayFullSize()
        {
            Debug.Assert(!_isLoadingFullSize);
            Debug.Assert(_fullsize != null);

            _image.SetStreamSource(_fullsize, Path.GetExtension(FullSize.AbsolutePath));

            _isShowingFullSize = true;
            _isShowingThumbnail = false;
        }
    }
}