using Microsoft.Phone.Controls;
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

        #region PlaceholderVisibility DependencyProperty

        public static readonly DependencyProperty PlaceholderVisibilityProperty = DependencyProperty.Register(
            "PlaceholderVisibility",
            typeof(Visibility),
            typeof(PlaceHolderLongListSelector),
            new PropertyMetadata(Visibility.Visible, OnPlaceholderVisibilityChanged));

        public Visibility PlaceholderVisibility
        {
            get { return (Visibility)GetValue(PlaceholderVisibilityProperty); }
            set { SetValue(PlaceholderVisibilityProperty, value); }
        }

        private static void OnPlaceholderVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as PlaceHolderLongListSelector).PlaceholderVisibilityChanged();
        }

        #endregion

        #region ItemsSource DependencyProperty

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

            Loaded += PlaceHolderLongListSelector_Loaded;
            Unloaded += PlaceHolderLongListSelector_Unloaded;
        }

        private void PlaceHolderLongListSelector_Loaded(object sender, RoutedEventArgs e)
        {
            if (ItemsSource is INotifyCollectionChanged)
            {
                (ItemsSource as INotifyCollectionChanged).CollectionChanged += UpdatePlaceholder;
                UpdatePlaceholder(null, null);
            }
        }

        private void PlaceHolderLongListSelector_Unloaded(object sender, RoutedEventArgs e)
        {
            if (ItemsSource is INotifyCollectionChanged)
            {
                (ItemsSource as INotifyCollectionChanged).CollectionChanged -= UpdatePlaceholder;
            }
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
            UpdatePlaceholder(null, null);
        }

        private void PlaceholderTextChanged()
        {
            if (_placeHolder != null)
            {
                _placeHolder.Text = PlaceholderText;
            }
        }

        private void PlaceholderVisibilityChanged()
        {
            UpdatePlaceholder(null, null);
        }

        private void UpdatePlaceholder(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_placeHolder != null && ItemsSource != null)
            {
                _placeHolder.Visibility = ItemsSource.Count == 0 ? PlaceholderVisibility : Visibility.Collapsed;
            }
        }
    }
}
