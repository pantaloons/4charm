using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using _4charm.Models;
using System.ComponentModel;
using System.Threading;

namespace _4charm.Controls
{
    public class StaticImage : Control, IPreloadedImage
    {
        private Image _container;
        private BitmapImage _loading;
        private BitmapImage _image;
        private Size? _size;

        private Stream _streamSource;

        public StaticImage()
        {
            DefaultStyleKey = typeof(StaticImage);

            SizeChanged += StaticImage_SizeChanged;
        }

        public Task<bool> SetStreamSource(Stream source, string fileType, CancellationToken token)
        {
            return LoadNew(source, token);
        }

        public void SetUriSource(Uri uri)
        {
            Debug.Assert(DesignerProperties.GetIsInDesignMode(this));

            _image = new BitmapImage() { UriSource = uri, CreateOptions = BitmapCreateOptions.None };
        }

        public void UnloadStreamSource()
        {
            Unload();
            if (_decodeTask != null)
            {
                UnloadLoading();
            }
            _streamSource = null;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _container = (Image)GetTemplateChild("ImageContainer");

            if (DesignerProperties.GetIsInDesignMode(this))
            {
                _container.Source = _image;
                return;
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _size = finalSize;

            return base.ArrangeOverride(finalSize);
        }

        private void StaticImage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize != e.PreviousSize)
            {
                LoadIfNeeded();
            }
        }

        private async Task<bool> LoadNew(Stream source, CancellationToken token)
        {
            BitmapImage bi = new BitmapImage()
            {
                CreateOptions = BitmapCreateOptions.BackgroundCreation,
                DecodePixelType = DecodePixelType.Logical,
                DecodePixelWidth = (int)_size.Value.Width
            };

            bool? success = await DecodeImage(bi, source);

            if (token.IsCancellationRequested)
            {
                return false;
            }

            if (success.Value)
            {
                Unload();
                _image = bi;
                _streamSource = source;

                if (_container != null)
                {
                    _container.Source = _image;

                    // Note: There is a SL bug when we set the _container.Source = null multiple times,
                    // and then assign it back to a valid BitmapImage on the same UI tick, it will not
                    // repaint. This has to stay here to force that action.
                    _container.InvalidateArrange();
                    InvalidateArrange();
                }
            }
            else
            {
                bi.UriSource = null;
                bi = null;
            }

            return success.Value;
        }

        private TaskCompletionSource<bool?> _decodeTask;
        private async Task<bool?> DecodeImage(BitmapImage bi, Stream source)
        {
            if (_decodeTask != null)
            {
                UnloadLoading();
            }

            _decodeTask = new TaskCompletionSource<bool?>();
            bi.ImageOpened += ImageOpened;
            bi.ImageFailed += ImageFailed;

            bi.SetSource(source);
            _loading = bi;

            bool? success = await _decodeTask.Task;

            if (success == null)
            {
                // Someone already replaced the decode task, so don't remove it
                return false;
            }

            _loading = null;

            return success;
        }

        private void ImageOpened(object sender, RoutedEventArgs e)
        {
            _decodeTask.SetResult(true);
            _decodeTask = null;
        }

        private void ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            _decodeTask.SetResult(false);
            _decodeTask = null;
        }

        private void LoadIfNeeded()
        {
            if (_container == null || _size == null || _streamSource == null || _container.Source != null)
            {
                return;
            }

            if (_image != null)
            {
                _container.Source = _image;
            }
            else
            {
                LoadNew(_streamSource, CancellationToken.None).ContinueWith(task =>
                {
                    throw task.Exception;
                }, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
            }
        }

        private void Unload()
        {
            if (_image != null)
            {
                _image.UriSource = null;
                _image = null;
                _container.Source = null;
            }
        }

        private void UnloadLoading()
        {
            _decodeTask.SetResult(null);
            _decodeTask = null;
            _loading.ImageOpened -= ImageOpened;
            _loading.ImageFailed -= ImageFailed;
            _loading.UriSource = null;
            _loading = null;
        }
    }
}
