using GIFSurface;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace _4charm.Controls.Image
{
    public class AnimatedImage : Control, IPreloadedImage
    {
        #region ShouldAnimate DependencyProperty

        public static readonly DependencyProperty ShouldAnimateProperty = DependencyProperty.Register(
            "ShouldAnimate",
            typeof(bool),
            typeof(AnimatedImage),
            new PropertyMetadata(false, OnShouldAnimateChanged));

        public bool ShouldAnimate
        {
            get { return (bool)GetValue(ShouldAnimateProperty); }
            set { SetValue(ShouldAnimateProperty, value); }
        }

        private static void OnShouldAnimateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as AnimatedImage).ShouldAnimateChanged();
        }

        #endregion

        private Size? _size;
        private Stream _streamSource;
        private GIFWrapper _gifWrapper;
        private string _fileType;
        private bool _hasCreatedProvider;

        private bool _isLoading;
        private CancellationTokenSource _cancel;

        private StaticImage _image;
        private DrawingSurface _surface;
        
        public AnimatedImage()
        {
            DefaultStyleKey = typeof(AnimatedImage);

            _gifWrapper = new GIFWrapper();

            SizeChanged += AnimatedImage_SizeChanged;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _image = (StaticImage)GetTemplateChild("ImageContainer");
            _surface = (DrawingSurface)GetTemplateChild("SurfaceContainer");

            CreateIfReady();
        }

        private void AnimatedImage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _size = e.NewSize;

            if (!_hasCreatedProvider)
            {
                CreateIfReady();
            }
            else
            {
                UpdateRendererSize();
            }
        }

        public Task SetStreamSource(Stream source, string fileType)
        {
            Debug.Assert(source != _streamSource);

            _fileType = fileType;
            _streamSource = source;
            if (_isLoading)
            {
                _isLoading = false;
                _cancel.Cancel();
                _cancel = null;
            }

            return ForceLoad();
        }

        public void UnloadStreamSource()
        {
            if (_isLoading)
            {
                _isLoading = false;
                _cancel.Cancel();
                _cancel = null;
            }

            if (_image != null)
            {
                _image.UnloadStreamSource();
            }
            _gifWrapper.UnloadGIF();
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

            if (_fileType == ".gif")
            {
                byte[] data = new byte[_streamSource.Length];
                _streamSource.Seek(0, SeekOrigin.Begin);
                _streamSource.Read(data, 0, (int)_streamSource.Length);

                _isLoading = true;
                GIFImage gif;
                try
                {
                    gif = await Task.Run<GIFImage>(() => new GIFImage(data));
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
                    gif.Dispose();
                    return;
                }

                _gifWrapper.SetGIFSource(gif);
                _image.Opacity = 0;
                _surface.Opacity = 1;
            }
            else
            {
                _isLoading = true;
                try
                {
                    await _image.SetStreamSource(_streamSource, _fileType);
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

                _image.Opacity = 1;
                _surface.Opacity = 0;
            }
        }

        private void ShouldAnimateChanged()
        {
            _gifWrapper.ShouldAnimate = ShouldAnimate;
        }

        private void CreateIfReady()
        {
            if (_surface == null || _size == null || _hasCreatedProvider)
            {
                return;
            }

            _hasCreatedProvider = true;
            UpdateRendererSize();
            _surface.SetContentProvider(_gifWrapper.CreateContentProvider());
        }

        private void UpdateRendererSize()
        {
            // Set window bounds in dips
            _gifWrapper.WindowBounds = new Windows.Foundation.Size(
                (float)_size.Value.Width,
                (float)_size.Value.Height
                );

            // Set native resolution in pixels
            _gifWrapper.NativeResolution = new Windows.Foundation.Size(
                (float)Math.Floor(_size.Value.Width *Application.Current.Host.Content.ScaleFactor / 100.0f + 0.5f),
                (float)Math.Floor(_size.Value.Height *Application.Current.Host.Content.ScaleFactor / 100.0f + 0.5f)
                );

            // Set render resolution to the full native resolution
            _gifWrapper.RenderResolution = _gifWrapper.NativeResolution;
        }
    }
}
