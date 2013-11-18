using GIFSurface;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace _4charm.Controls
{
    public class GIFImage : Control, IPreloadedImage
    {
        private Stream _streamSource;
        public Stream StreamSource
        {
            get { return _streamSource; }
            set
            {
                Unload();
                _streamSource = value;
                LoadIfNeeded();
            }
        }

        public int PixelWidth
        {
            get { return 0; }
        }

        public int PixelHeight
        {
            get { return 0; }
        }

        private DrawingSurface _surface;
        private GIFWrapper _gifWrapper;
        private Size? _size;

        public GIFImage()
        {
            DefaultStyleKey = typeof(GIFImage);

            Loaded += GIFImage_Loaded;
            Unloaded += GIFImage_Unloaded;
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

            _surface = (DrawingSurface)GetTemplateChild("SurfaceContainer");

            LoadIfNeeded();
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _size = finalSize;
            LoadIfNeeded();

            return base.ArrangeOverride(finalSize);
        }

        private void LoadIfNeeded()
        {
            if (_surface == null || _size == null || StreamSource == null || _gifWrapper != null)
            {
                return;
            }

            try
            {
                _gifWrapper = new GIFWrapper();
            }
            catch
            {
                Unload();
                throw;
            }

            // Set window bounds in dips
            _gifWrapper.WindowBounds = new Windows.Foundation.Size(
                (float)_surface.ActualWidth,
                (float)_surface.ActualHeight
                );

            // Set native resolution in pixels
            _gifWrapper.NativeResolution = new Windows.Foundation.Size(
                (float)Math.Floor(_surface.ActualWidth * Application.Current.Host.Content.ScaleFactor / 100.0f + 0.5f),
                (float)Math.Floor(_surface.ActualHeight * Application.Current.Host.Content.ScaleFactor / 100.0f + 0.5f)
                );

            // Set render resolution to the full native resolution
            _gifWrapper.RenderResolution = _gifWrapper.NativeResolution;

            _surface.SetContentProvider(_gifWrapper.CreateContentProvider());
        }

        private void Unload()
        {
            if (_gifWrapper != null)
            {
                //_gifWrapper.Unload();
                _gifWrapper = null;

                _surface.SetContentProvider(null);
            }
        }
    }
}
