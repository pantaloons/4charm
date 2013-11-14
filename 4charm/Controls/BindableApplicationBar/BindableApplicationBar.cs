using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using PhotosApp.Helpers;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace PhotosApp.Controls
{
    public class BindableApplicationBar : ItemsControl
    {
        private IApplicationBar _applicationBar = new ApplicationBar();

        public IApplicationBar ApplicationBar
        {
            get { return _applicationBar; }
        }

        public static readonly DependencyProperty ApplicationBarModeProperty = DependencyProperty.Register(
            "ApplicationBarMode",
            typeof(ApplicationBarMode),
            typeof(BindableApplicationBar),
            new PropertyMetadata(OnApplicationBarModePropertyChanged));

        public ApplicationBarMode ApplicationBarMode
        {
            get { return (ApplicationBarMode)GetValue(ApplicationBarModeProperty); }
            set { SetValue(ApplicationBarModeProperty, value); }
        }

        public static readonly DependencyProperty IsApplicationBarOpenedProperty = DependencyProperty.Register(
            "IsApplicationBarOpened",
            typeof(bool),
            typeof(BindableApplicationBar),
            new PropertyMetadata(false));

        public bool IsApplicationBarOpened
        {
            get { return (bool)GetValue(IsApplicationBarOpenedProperty); }
            private set { SetValue(IsApplicationBarOpenedProperty, value); }
        }
        
        public static readonly DependencyProperty IsMenuEnabledProperty = DependencyProperty.Register(
            "IsMenuEnabled",
            typeof(bool),
            typeof(BindableApplicationBar),
            new PropertyMetadata(true, OnIsMenuEnabledPropertyChanged));

        public bool IsMenuEnabled
        {
            get { return (bool)GetValue(IsMenuEnabledProperty); }
            set { SetValue(IsMenuEnabledProperty, value); }
        }

        public event EventHandler Tapped;

        public BindableApplicationBar()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            _applicationBar.StateChanged += OnApplicationBarStateChanged;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.IsInDesignTool == false)
            {
                LoadItems();
                ApplyVisibility();
                ApplyOpacity();
                ApplyBackground();
            }

            // Subscribe to background changes
            //
            Binding backgroundBinding = new Binding("Background");
            backgroundBinding.Source = this;
            DependencyProperty backgroundProperty = DependencyProperty.RegisterAttached(
                "BackgroundProxyProperty",
                typeof(Brush),
                typeof(BindableApplicationBar),
                new PropertyMetadata(OnBackgroundChanged));
            SetBinding(backgroundProperty, backgroundBinding);

            // Subscribe to opacity changes
            //
            Binding opacityBinding = new Binding("Opacity");
            opacityBinding.Source = this;
            DependencyProperty opacityProperty = DependencyProperty.RegisterAttached(
                "OpacityProxyProperty",
                typeof(double),
                typeof(BindableApplicationBar),
                new PropertyMetadata(OnOpacityChanged));
            SetBinding(opacityProperty, opacityBinding);

            // Subscribe to visibility changes
            //
            Binding visibilityBinding = new Binding("Visibility");
            visibilityBinding.Source = this;
            DependencyProperty visiblityProperty = DependencyProperty.RegisterAttached(
                "VisibilityProxyProperty",
                typeof(object),
                typeof(BindableApplicationBar),
                new PropertyMetadata(OnVisibilityChanged));
            SetBinding(visiblityProperty, visibilityBinding);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            UnloadItems();
        }

        private static void OnBackgroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BindableApplicationBar me = (BindableApplicationBar)d;
            me.ApplyBackground();
        }

        private static void OnVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BindableApplicationBar me = (BindableApplicationBar)d;
            me.ApplyVisibility();
        }

        private static void OnOpacityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BindableApplicationBar me = (BindableApplicationBar)d;
            me.ApplyOpacity();
        }

        private static void OnApplicationBarModePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BindableApplicationBar me = (BindableApplicationBar)d;
            me._applicationBar.Mode = (ApplicationBarMode)e.NewValue;
        }

        private void OnApplicationBarStateChanged(object sender, ApplicationBarStateChangedEventArgs e)
        {
            IsApplicationBarOpened = e.IsMenuVisible;
        }

        private static void OnIsMenuEnabledPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BindableApplicationBar me = (BindableApplicationBar)d;
            me._applicationBar.IsMenuEnabled = (bool)e.NewValue;
        }

        private void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "EffectiveVisibility")
            {
                OnItemEffectiveVisibilityChanged(sender);
            }
        }

        private void OnItemEffectiveVisibilityChanged(object item)
        {
            if (item is BindableApplicationBarIconButton)
            {
                BindableApplicationBarIconButton button = (BindableApplicationBarIconButton)item;

                if (button.EffectiveVisibility == Visibility.Visible)
                {
                    _applicationBar.Buttons.Insert(
                        GetVisibleIndex<BindableApplicationBarIconButton>(Items, button),
                        button.ApplicationBarIconButton);
                }
                else
                {
                    _applicationBar.Buttons.Remove(button.ApplicationBarIconButton);
                }
            }
            else if (item is BindableApplicationBarMenuItem)
            {
                BindableApplicationBarMenuItem menuItem = (BindableApplicationBarMenuItem)item;

                if (menuItem.EffectiveVisibility == Visibility.Visible)
                {
                    _applicationBar.MenuItems.Insert(
                        GetVisibleIndex<BindableApplicationBarMenuItem>(Items, menuItem),
                        menuItem.ApplicationBarMenuItem);
                }
                else
                {
                    _applicationBar.MenuItems.Remove(menuItem.ApplicationBarMenuItem);
                }
            }
        }

        private void RemoveButtonOrMenuItem(BindableApplicationBarItemBase item)
        {
            if (item is BindableApplicationBarIconButton)
            {
                _applicationBar.Buttons.Remove(((BindableApplicationBarIconButton)item).ApplicationBarIconButton);
            }
            else if (item is BindableApplicationBarMenuItem)
            {
                _applicationBar.MenuItems.Remove(((BindableApplicationBarMenuItem)item).ApplicationBarMenuItem);
            }
        }

        private void ApplyVisibility()
        {
            _applicationBar.IsVisible = (Visibility)Visibility == Visibility.Visible;
        }

        private void ApplyOpacity()
        {
            _applicationBar.Opacity = Opacity;
        }

        private void ApplyBackground()
        {
            if (Background is SolidColorBrush)
            {
                _applicationBar.BackgroundColor = ((SolidColorBrush)Background).Color;
            }
            else if (Background != null)
            {
                throw new ArgumentException("The Background property of a BindableApplicationBar may only be a SolidColorBrush");
            }
        }

        private void LoadItems()
        {
            _applicationBar.Buttons.Clear();
            _applicationBar.MenuItems.Clear();

            if (DesignerProperties.IsInDesignTool == false)
            {
                foreach (BindableApplicationBarItemBase item in Items)
                {
                    item.PropertyChanged += OnItemPropertyChanged;
                    item.Tapped += OnItemTapped;

                    if (item.EffectiveVisibility == Visibility.Visible)
                    {
                        if (item is BindableApplicationBarIconButton)
                        {
                            // NOTE: If you get an 'System.ArgumentNullException' here, check to see if you gave the button an IconUri
                            _applicationBar.Buttons.Add(((BindableApplicationBarIconButton)item).ApplicationBarIconButton);
                        }
                        else if (item is BindableApplicationBarMenuItem)
                        {
                            _applicationBar.MenuItems.Add(((BindableApplicationBarMenuItem)item).ApplicationBarMenuItem);
                        }
                    }
                }
            }
        }

        private void OnItemTapped(object sender, EventArgs e)
        {
            if (Tapped != null)
            {
                Tapped(null, EventArgs.Empty);
            }
        }

        private void UnloadItems()
        {
            _applicationBar.Buttons.Clear();
            _applicationBar.MenuItems.Clear();

            if (DesignerProperties.IsInDesignTool == false)
            {
                foreach (BindableApplicationBarItemBase item in Items)
                {
                    item.PropertyChanged -= OnItemPropertyChanged;
                    item.Tapped -= OnItemTapped;
                }
            }
        }

        /// <summary>
        /// Finds in the index of the item in the list, ignoring list members that are collapsed
        /// </summary>
        /// <typeparam name="T">The type of the items in the list</typeparam>
        /// <param name="list">The list to search</param>
        /// <param name="item">The item to look for</param>
        private int GetVisibleIndex<T>(ItemCollection list, T item) where T : BindableApplicationBarItemBase
        {
            int visibleItemCount = 0;

            for (int index = 0; index < list.Count; index++)
            {
                if (list[index].Equals(item))
                {
                    return visibleItemCount;
                }
                if ((list[index] is T) && 
                    (((BindableApplicationBarItemBase)list[index]).EffectiveVisibility == Visibility.Visible))
                {
                    visibleItemCount++;
                }
            }

            throw new InvalidOperationException("The list did not contain the item supplied and expected to be in the list.");
        }
    }
}
