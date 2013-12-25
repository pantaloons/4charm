using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace _4charm.Models
{
    public class ImageUtils
    {
        public static Task<BitmapImage> DecodeImageAsync(Stream source, int decodePixelWidth)
        {
            return new AsyncBitmapImage().DecodeImageAsync(source, decodePixelWidth);
        }

        public static Task<BitmapImage> DecodeImageAsync(BitmapImage target, Stream source, int decodePixelWidth)
        {
            return new AsyncBitmapImage().DecodeImageAsync(target, source, decodePixelWidth);
        }

        private class AsyncBitmapImage
        {
            private BitmapImage _bitmapImage;
            private GCHandle _handle;
            private TaskCompletionSource<BitmapImage> _tcs;

            public Task<BitmapImage> DecodeImageAsync(Stream source, int decodePixelWidth)
            {
                _bitmapImage = new BitmapImage()
                {
                    CreateOptions = BitmapCreateOptions.BackgroundCreation,
                    DecodePixelType = DecodePixelType.Logical,
                    DecodePixelWidth = (int)decodePixelWidth
                };

                _tcs = new TaskCompletionSource<BitmapImage>();
                _bitmapImage.ImageOpened += ImageOpened;
                _bitmapImage.ImageFailed += ImageFailed;

                _handle = GCHandle.Alloc(this, GCHandleType.Normal);
                _bitmapImage.SetSource(source);

                return _tcs.Task;
            }

            public Task<BitmapImage> DecodeImageAsync(BitmapImage target, Stream source, int decodePixelWidth)
            {
                _bitmapImage = target;

                target.CreateOptions = BitmapCreateOptions.BackgroundCreation;
                target.DecodePixelType = DecodePixelType.Logical;
                target.DecodePixelWidth = decodePixelWidth;

                _tcs = new TaskCompletionSource<BitmapImage>();
                target.ImageOpened += ImageOpened;
                target.ImageFailed += ImageFailed;

                _handle = GCHandle.Alloc(this);
                target.SetSource(source);

                return _tcs.Task;
            }

            private void ImageOpened(object sender, RoutedEventArgs e)
            {
                _bitmapImage.ImageOpened -= ImageOpened;
                _bitmapImage.ImageFailed -= ImageFailed;

                _tcs.SetResult((BitmapImage)sender);
                _handle.Free();
            }

            private void ImageFailed(object sender, ExceptionRoutedEventArgs e)
            {
                _bitmapImage.ImageOpened -= ImageOpened;
                _bitmapImage.ImageFailed -= ImageFailed;

                _tcs.SetException(e.ErrorException);
                _handle.Free();
            }
        }
    }
}
