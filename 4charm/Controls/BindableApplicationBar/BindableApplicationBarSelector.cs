using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PhotosApp.Controls
{
    public class BindableApplicationBarSelector : ItemsControl
    {
        private bool _loaded = false;
        private PhoneApplicationPage _page = null;

        public static readonly DependencyProperty SelectedApplicationBarProperty = DependencyProperty.Register(
            "SelectedApplicationBar",
            typeof(string),
            typeof(BindableApplicationBarSelector),
            new PropertyMetadata(null, OnSelectedApplicationBarPropertyChanged));

        public string SelectedApplicationBar
        {
            get { return (string)GetValue(SelectedApplicationBarProperty); }
            set { SetValue(SelectedApplicationBarProperty, value); }
        }


        public BindableApplicationBarSelector()
        {            
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            DependencyObject pageCandidate = this;

            // Search up visual tree for the PhoneApplicationPage
            while (pageCandidate is PhoneApplicationPage == false)
            {
                pageCandidate = VisualTreeHelper.GetParent(pageCandidate);
            }

            _page = (PhoneApplicationPage)pageCandidate;
            _loaded = true;
            ApplySelection();
        }

        private static void OnSelectedApplicationBarPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BindableApplicationBarSelector me = (BindableApplicationBarSelector)d;
            me.ApplySelection();
        }

        private void ApplySelection()
        {
            if (_loaded == false)
            {
                // We don't have access to the new page until we have been loaded, at which point we'll be called again
                return;
            }

            BindableApplicationBar selectedBindableApplicationBar = null;

            foreach (BindableApplicationBar appBar in Items)
            {
                if (appBar.Name.Equals(SelectedApplicationBar))
                {
                    selectedBindableApplicationBar = appBar;
                    break;
                }
            }

            if (selectedBindableApplicationBar != null)
            {
                _page.ApplicationBar = selectedBindableApplicationBar.ApplicationBar;
            }
            else
            {
                _page.ApplicationBar = null;
            }
        }
    }
}
