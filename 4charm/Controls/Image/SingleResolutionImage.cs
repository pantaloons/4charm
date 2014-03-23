using _4charm.Models;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Windows.Storage;

namespace _4charm.Controls.Image
{
    public class SingleResolutionImage : StaticImage
    {
        #region ImageUri DependencyProperty

        public static readonly DependencyProperty ImageURIProperty = DependencyProperty.Register(
            "ImageURI",
            typeof(Uri),
            typeof(SingleResolutionImage),
            new PropertyMetadata(null, OnImageURIChanged));

        public Uri ImageURI
        {
            get { return (Uri)GetValue(ImageURIProperty); }
            set { SetValue(ImageURIProperty, value); }
        }

        private static void OnImageURIChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as SingleResolutionImage).ImageURIChanged();
        }

        #endregion

        private bool _isLoading;
        private CancellationTokenSource _cancel;

        public override void OnApplyTemplate()
        {
            if (DesignerProperties.GetIsInDesignMode(this))
            {
                // In design mode, we must set the UriSource directly.
                System.Windows.Controls.Image container = GetTemplateChild("ImageContainer") as System.Windows.Controls.Image;
                if (container != null)
                {
                    // Remove the name so the child StaticImage doesn't find it.
                    container.Name = null;
                    container.Source = new BitmapImage() { UriSource = ImageURI };
                }
            }

            base.OnApplyTemplate();
        }

        private void ImageURIChanged()
        {
            if (_isLoading)
            {
                _isLoading = false;
                _cancel.Cancel();
                _cancel = null;
            }
            UnloadStreamSource();

            if (ImageURI == null)
            {    
                return;
            }

            _cancel = new CancellationTokenSource();
            CancellationToken token = _cancel.Token;

            _isLoading = true;
            GetURIStream(ImageURI).ContinueWith(task =>
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }
                else if (task.IsFaulted)
                {
                    Debug.Assert(_cancel.Token == token);
                    _cancel = null;
                    _isLoading = false;
                    return;
                }

                SetStreamSource(task.Result, null).ContinueWith(task2 =>
                {
                    if (!token.IsCancellationRequested)
                    {
                        Debug.Assert(_cancel.Token == token);
                        _cancel = null;
                        _isLoading = false;
                    }
                }, TaskContinuationOptions.ExecuteSynchronously);                
            }, TaskContinuationOptions.ExecuteSynchronously);
        }

        private async Task<Stream> GetURIStream(Uri uri)
        {
            if (uri.Scheme == "file")
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(uri.LocalPath);
                return await file.OpenStreamForReadAsync();
            }
            else
            {
                return new MemoryStream(await RequestManager.Current.GetByteArrayAsync(uri));
            }
        }
    }
}
