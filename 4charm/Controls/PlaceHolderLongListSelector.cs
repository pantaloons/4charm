using Microsoft.Phone.Controls;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace _4charm.Controls
{
    public class PlaceHolderLongListSelector : LongListSelector
    {
        #region PlaceholderText DependencyProperty

        public static readonly DependencyProperty PlaceholderTextProperty = DependencyProperty.Register(
            "PlaceholderText",
            typeof(string),
            typeof(PlaceHolderLongListSelector),
            new PropertyMetadata(null, OnPlaceholderTextChanged));

        public string PlaceholderText
        {
            get { return (string)GetValue(PlaceholderTextProperty); }
            set { SetValue(PlaceholderTextProperty, value); }
        }

        private static void OnPlaceholderTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as PlaceHolderLongListSelector).PlaceholderTextChanged();
        }

        #endregion

        #region PlaceholderText DependencyProperty

        public static readonly new DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            "ItemsSource",
            typeof(IList),
            typeof(PlaceHolderLongListSelector),
            new PropertyMetadata(null, OnItemsSourceChanged));

        public new IList ItemsSource
        {
            get { return (IList)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as PlaceHolderLongListSelector).ItemsSourceChanged(e);
        }

        #endregion

        private TextBlock _placeHolder;

        public PlaceHolderLongListSelector()
        {
            DefaultStyleKey = typeof(PlaceHolderLongListSelector);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _placeHolder = (TextBlock)GetTemplateChild("Placeholder");

            PlaceholderTextChanged();
            UpdatePlaceholder(null, null);
        }

        private void ItemsSourceChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is INotifyCollectionChanged)
            {
                (e.OldValue as INotifyCollectionChanged).CollectionChanged -= UpdatePlaceholder;
            }

            if (ItemsSource is INotifyCollectionChanged)
            {
                (ItemsSource as INotifyCollectionChanged).CollectionChanged += UpdatePlaceholder;
            }

            base.ItemsSource = ItemsSource;
        }

        private void PlaceholderTextChanged()
        {
            if (_placeHolder != null)
            {
                _placeHolder.Text = PlaceholderText;
            }
        }

        private void UpdatePlaceholder(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_placeHolder != null)
            {
                _placeHolder.Visibility = ItemsSource.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }
}
