using GIFSurface;
using Microsoft.Phone.Controls;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace _4charm.Controls
{
    public class GIFImage : Control, IPreloadedImage
    {
        #region ShouldAnimate DependencyProperty

        public static readonly DependencyProperty ShouldAnimateProperty = DependencyProperty.Register(
            "ShouldAnimate",
            typeof(bool),
            typeof(GIFImage),
            new PropertyMetadata(false, OnShouldAnimateChanged));

        public bool ShouldAnimate
        {
            get { return (bool)GetValue(ShouldAnimateProperty); }
            set { SetValue(ShouldAnimateProperty, value); }
        }

        private static void OnShouldAnimateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as GIFImage).ShouldAnimateChanged();
        }

        #endregion

        private StaticImage _image;
        private DrawingSurface _surface;
        private GIFWrapper _gifWrapper;
        private Size? _size;

        private Stream _streamSource;
        private string _fileType;
        private bool _hasLoadedGif;
        private bool _hasCreatedProvider;

        public GIFImage()
        {
            DefaultStyleKey = typeof(GIFImage);

            _gifWrapper = new GIFWrapper();

            SizeChanged += GIFImage_SizeChanged;
        }

        public Task<bool> SetStreamSource(Stream source, string fileType, CancellationToken token)
        {
            // Don't do a full unload, since we want to keep
            // the previous image instance until we overwrite
            // it, else there will be a black flash
            _gifWrapper.UnloadGIF();
            _hasLoadedGif = false;

            return LoadNew(source, fileType, token);
        }

        public void UnloadStreamSource()
        {
            Unload();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _image = (StaticImage)GetTemplateChild("ImageContainer");
            _surface = (DrawingSurface)GetTemplateChild("SurfaceContainer");

            CreateIfReady();
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _size = finalSize;

            return base.ArrangeOverride(finalSize);
        }

        private void GIFImage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!_hasCreatedProvider)
            {
                CreateIfReady();
            }
            else
            {
                UpdateRendererSize();
            }
        }

        private void ShouldAnimateChanged()
        {
            _gifWrapper.SetShouldAnimate(ShouldAnimate);
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

        private async Task<bool> LoadNew(Stream source, string fileType, CancellationToken token)
        {
            if (fileType == ".gif")
            {
                bool success = await LoadGIF(source);

                if (token.IsCancellationRequested)
                {
                    return false;
                }

                if (success)
                {
                    _hasLoadedGif = true;
                    _image.Opacity = 0;
                    _surface.Opacity = 1;
                    _streamSource = source;
                    _fileType = fileType;
                }

                return success;
            }
            else
            {
                bool success = await _image.SetStreamSource(source, fileType, token);

                if (token.IsCancellationRequested)
                {
                    return false;
                }

                if (success)
                {
                    _image.Opacity = 1;
                    _surface.Opacity = 0;
                    _streamSource = source;
                    _fileType = fileType;
                }

                return success;
            }
        }

        private async Task<bool> LoadGIF(Stream source)
        {
            byte[] data = new byte[source.Length];
            source.Seek(0, SeekOrigin.Begin);
            source.Read(data, 0, (int)source.Length);

            try
            {
                await _gifWrapper.SetGIF(data, ShouldAnimate);
            }
            catch
            {
                Unload();
                return false;
            }

            return true;
        }

        private void Unload()
        {
            if (_image != null)
            {
                _image.UnloadStreamSource();
            }
            _gifWrapper.UnloadGIF();
            _hasLoadedGif = false;
        }
    }
}
