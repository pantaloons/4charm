using GIFSurface;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

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

        public int PixelWidth
        {
            get { return 0; }
        }

        public int PixelHeight
        {
            get { return 0; }
        }

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

            Loaded += GIFImage_Loaded;
            Unloaded += GIFImage_Unloaded;
        }

        public void SetStreamSource(Stream source, string fileType)
        {
            Unload();
            _streamSource = source;
            _fileType = fileType;
            LoadIfNeeded();
        }

        public void UnloadStreamSource()
        {
            Unload();
        }

        private void GIFImage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadIfNeeded();
        }

        private void GIFImage_Unloaded(object sender, RoutedEventArgs e)
        {
            Unload();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _image = (StaticImage)GetTemplateChild("ImageContainer");
            _surface = (DrawingSurface)GetTemplateChild("SurfaceContainer");

            CreateIfReady();
            LoadIfNeeded();
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _size = finalSize;

            if (!_hasCreatedProvider)
            {
                CreateIfReady();
            }
            else
            {
                UpdateRendererSize();
            }
            LoadIfNeeded();

            return base.ArrangeOverride(finalSize);
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

        private void LoadIfNeeded()
        {
            if (_surface == null || _size == null || _streamSource == null || _hasLoadedGif)
            {
                return;
            }

            if (_fileType == ".gif")
            {
                _image.UnloadStreamSource();
                _image.Opacity = 0;
                _surface.Opacity = 1;

                byte[] data = new byte[_streamSource.Length];
                _streamSource.Seek(0, SeekOrigin.Begin);
                _streamSource.Read(data, 0, (int)_streamSource.Length);

                try
                {
                    _gifWrapper.SetGIF(data, ShouldAnimate);
                }
                catch
                {
                    Unload();
                    throw;
                }

                _hasLoadedGif = true;
            }
            else
            {
                _gifWrapper.UnloadGIF();
                _image.Opacity = 1;
                _surface.Opacity = 0;

                _image.SetStreamSource(_streamSource, _fileType);
                _hasLoadedGif = true;
            }
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
