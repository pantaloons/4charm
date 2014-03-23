using _4charm.Models;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace _4charm.Controls.Image
{
    public class StaticImage : Control, IPreloadedImage
    {
        private Size? _size;
        private Stream _streamSource;

        private System.Windows.Controls.Image _container;
        private BitmapImage _image;

        private bool _isLoading;
        private CancellationTokenSource _cancel;

        public StaticImage()
        {
            DefaultStyleKey = typeof(StaticImage);

            SizeChanged += StaticImage_SizeChanged;
            Loaded += StaticImage_Loaded;
            Unloaded += StaticImage_Unloaded;
        }

        private void StaticImage_Loaded(object sender, RoutedEventArgs e)
        {
            ReloadStreamSource();
        }

        private void StaticImage_Unloaded(object sender, RoutedEventArgs e)
        {
            UnloadStreamSource(false);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _container = (System.Windows.Controls.Image)GetTemplateChild("ImageContainer");

            UpdateSource();
        }

        private void StaticImage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _size = e.NewSize;

            if (e.NewSize != e.PreviousSize)
            {
                if (_isLoading)
                {
                    _isLoading = false;
                    _cancel.Cancel();
                    _cancel = null;
                }

                ForceLoad().ContinueWith(task =>
                {
                    throw task.Exception;
                }, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        public Task SetStreamSource(Stream source, string fileType)
        {
            Debug.Assert(source != _streamSource);

            _streamSource = source;
            if (_isLoading)
            {
                _isLoading = false;
                _cancel.Cancel();
                _cancel = null;
            }

            return ForceLoad();
        }

        private void ReloadStreamSource()
        {
            if (_streamSource == null || _isLoading || _image != null)
            {
                return;
            }

            ForceLoad().ContinueWith(task =>
            {
                throw task.Exception;
            }, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnFaulted);
        }

        public void UnloadStreamSource()
        {
            UnloadStreamSource(true);
        }

        private void UnloadStreamSource(bool removeStream)
        {
            if (_isLoading)
            {
                _isLoading = false;
                _cancel.Cancel();
                _cancel = null;
            }

            if (_image != null)
            {
                _image.UriSource = null;
                _image = null;                
            }

            if (removeStream)
            {
                _streamSource = null;
            }

            //if (_container != null)
            //{
            //    _container.Source = null;
            //}
        }

        private async Task ForceLoad()
        {
            if (_streamSource == null || _size == null)
            {
                return;
            }

            Debug.Assert(!_isLoading);
            Debug.Assert(_cancel == null);

            _cancel = new CancellationTokenSource();
            CancellationToken token = _cancel.Token;

            if (_image == null)
            {
                _isLoading = true;

                BitmapImage bi;
                try
                {
                    bi = await ImageUtils.DecodeImageAsync(_streamSource, (int)_size.Value.Width);
                }
                catch
                {
                    return;
                }
                finally
                {
                    if (!token.IsCancellationRequested)
                    {
                        Debug.Assert(_cancel.Token == token);
                        _cancel = null;
                        _isLoading = false;
                    }
                }

                if (token.IsCancellationRequested)
                {
                    // The source got changed again, after this was called.
                    bi.UriSource = null;
                    return;
                }

                Debug.Assert(_image == null);

                _image = bi;
                UpdateSource();
            }
            else
            {
                // Replace the existing image in-place, to save some memory.
                _isLoading = true;

                BitmapImage bi;
                try
                {
                    bi = await ImageUtils.DecodeImageAsync(_streamSource, (int)_size.Value.Width);
                }
                catch
                {
                    return;
                }
                finally
                {
                    if (!token.IsCancellationRequested)
                    {
                        Debug.Assert(_cancel.Token == token);
                        _cancel = null;
                        _isLoading = false;
                    }
                }

                if (token.IsCancellationRequested)
                {
                    // The source got changed again, after this was called.
                    bi.UriSource = null;
                    return;
                }

                Debug.Assert(_image != null);
                _image.UriSource = null;
                _image = null;

                if (_container != null)
                {
                    _container.Source = null;
                }

                _image = bi;
                UpdateSource();
            }
        }

        private void UpdateSource()
        {
            if (_container != null && _image != null)
            {
                //Debug.Assert(_container.Source == null);

                _container.Source = _image;
            }
        }
    }
}
