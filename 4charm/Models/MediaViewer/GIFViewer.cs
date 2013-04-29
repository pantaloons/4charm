using _4charm;
using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Microsoft.Phone.Controls
{
    public class GIFViewer : Control, INotifyPropertyChanged
    {
        private WebBrowser _browser;
        private double _w, _h;

        public GIFViewer()
        {
            DefaultStyleKey = typeof(GIFViewer);

            DependencyProperty dataContextDependencyProperty = System.Windows.DependencyProperty.RegisterAttached("DataContextProperty", typeof(object), typeof(FrameworkElement), new System.Windows.PropertyMetadata(OnDataContextPropertyChanged));
            SetBinding(dataContextDependencyProperty, new Binding());
        }

        private static void OnDataContextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            GIFViewer gif = (GIFViewer)d;

            gif.Unload();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _browser = GetTemplateChild("Browser") as WebBrowser;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            _w = availableSize.Width;
            _h = availableSize.Height;
            Display();
            return base.MeasureOverride(availableSize);
        }

        private double BrowserWidth
        {
            get
            {
                IDisplayableImage idi = DataContext as IDisplayableImage;
                if (_w == 0 || _h == 0) return 0;
                if (idi.ImageWidth / _w >= idi.ImageHeight / _h)
                {
                    return _w;
                }
                else
                {
                    double factor = BrowserHeight / idi.ImageHeight;
                    return idi.ImageWidth * factor;
                }
            }
        }

        private double BrowserHeight
        {
            get
            {
                IDisplayableImage idi = DataContext as IDisplayableImage;
                if (_w == 0 || _h == 0) return 0;
                if (idi.ImageWidth / _w < idi.ImageHeight / _h)
                {
                    return _h;
                }
                else
                {
                    double factor = BrowserWidth / idi.ImageWidth;
                    return idi.ImageHeight * factor;
                }
            }
        }

        public void Show(Action after)
        {
            if (_browser != null)
            {
                _browser.Visibility = System.Windows.Visibility.Visible;
                Task.Run(() =>
                {
                    //System.Threading.Thread.Sleep(1000);
                    Dispatcher.BeginInvoke(() =>
                    {
                        after();
                    });
                });
            }
        }

        public void Collapse()
        {
            if (_browser != null)
            {
                _browser.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        public void Display()
        {
            if (!(DataContext is IDisplayableImage)) return;

            IDisplayableImage image = DataContext as IDisplayableImage;
            double _w = image.ImageWidth;
            double _h = image.ImageHeight;
            var w = BrowserWidth;
            var h = BrowserHeight;

            string bg = "#" + (Application.Current.Resources["PhoneContrastForegroundBrush"] as SolidColorBrush).Color.ToString().Substring(3);
            string head = "<head><meta name=\"viewport\" content=\"width=" + w + ", height=" + h + "\"></head><body style='margin:0; padding:0; background-color: " + bg + ";'>";
            string body = "<img width='" + w + "' height='" + h + "' style='margin:0; padding:0' src=\"" + image.ImageSrc + "\"/>";
            string foot = "</body>";
            _browser.Width = w;
            _browser.Height = h;
            _browser.NavigateToString(head + body + foot);
        }

        public void Unload()
        {
            if (_browser != null)
            {
                while (_browser.CanGoBack) _browser.GoBack();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
