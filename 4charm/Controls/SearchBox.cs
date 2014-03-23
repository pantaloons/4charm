using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace _4charm.Controls
{
    public class SearchBox : SelectionBindableTextBox
    {
        #region IsVisible DependencyProperty

        public static readonly DependencyProperty IsVisibleProperty = DependencyProperty.Register(
            "IsVisible",
            typeof(bool),
            typeof(SearchBox),
            new PropertyMetadata(false, OnIsVisibleChanged));

        public bool IsVisible
        {
            get { return (bool)GetValue(IsVisibleProperty); }
            set { SetValue(IsVisibleProperty, value); }
        }

        private static void OnIsVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as SearchBox).IsVisibleChanged();
        }

        #endregion

        public SearchBox()
        {
            DefaultStyleKey = typeof(SearchBox);
        }

        private void IsVisibleChanged()
        {
            if (IsVisible)
            {
                VisualStateManager.GoToState(this, "Visible", true);
            }
            else
            {
                VisualStateManager.GoToState(this, "Invisible", true);
            }
        }
    }
}
