using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace _4charm.Controls
{
    public class StaticImage : Control, IPreloadedImage
    {
        public int PixelWidth
        {
            get { return _image.PixelWidth; }
        }

        public int PixelHeight
        {
            get { return _image.PixelHeight; }
        }

        private Image _container;
        private BitmapImage _image;
        private Size? _size;

        private Stream _streamSource;
        private string _fileType;

        public StaticImage()
        {
            DefaultStyleKey = typeof(StaticImage);

            Loaded += StaticImage_Loaded;
            Unloaded += StaticImage_Unloaded;
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

        private void StaticImage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadIfNeeded();
        }

        private void StaticImage_Unloaded(object sender, RoutedEventArgs e)
        {
            Unload();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _container = (Image)GetTemplateChild("ImageContainer");

            LoadIfNeeded();
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _size = finalSize;
            LoadIfNeeded();
            UpdateDecodeSize();

            return base.ArrangeOverride(finalSize);
        }

        private void LoadIfNeeded()
        {
            if (_container == null || _size == null || _streamSource == null || _image != null)
            {
                return;
            }

            _image = new BitmapImage()
            {
                CreateOptions = BitmapCreateOptions.DelayCreation,
                DecodePixelType = DecodePixelType.Logical,
                DecodePixelWidth = (int)_size.Value.Width
            };
            try
            {
                _image.SetSource(_streamSource);
            }
            catch
            {
                Unload();
                throw;
            }

            _container.Source = _image;
        }

        private void UpdateDecodeSize()
        {
            if (_image != null)
            {
                _image.DecodePixelWidth = (int)_size.Value.Width;
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
    }
}
