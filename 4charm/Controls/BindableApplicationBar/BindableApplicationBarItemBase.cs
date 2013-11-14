using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace PhotosApp.Controls
{
    public abstract class BindableApplicationBarItemBase : FrameworkElement, INotifyPropertyChanged
    {
        private Visibility _effectiveVisibility = Visibility.Visible;

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            "Text",
            typeof(string),
            typeof(BindableApplicationBarItemBase),
            new PropertyMetadata("", OnTextPropertyChanged));

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
            "Command",
            typeof(ICommand),
            typeof(BindableApplicationBarItemBase),
            new PropertyMetadata(null, OnCommandPropertyChanged));

        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        public static readonly DependencyProperty HideWhenDisabledProperty = DependencyProperty.Register(
            "HideWhenDisabled",
            typeof(bool),
            typeof(BindableApplicationBarItemBase),
            new PropertyMetadata(false, OnHideWhenDisabledPropertyChanged));

        public bool HideWhenDisabled
        {
            get { return (bool)GetValue(HideWhenDisabledProperty); }
            set { SetValue(HideWhenDisabledProperty, value); }
        }

        public Visibility EffectiveVisibility
        {
            get
            {
                return _effectiveVisibility;
            }
            private set
            {
                if (_effectiveVisibility != value)
                {
                    _effectiveVisibility = value;
                    if (PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs("EffectiveVisibility"));
                    }
                }
            }
        }

        private void CalculateEffectiveVisibility()
        {
            if (Visibility == Visibility.Collapsed)
            {
                EffectiveVisibility = Visibility.Collapsed;
            }
            else if ((Command != null) &&
                     (Command.CanExecute(null) == false) &&
                     (HideWhenDisabled == true))
            {
                EffectiveVisibility = Visibility.Collapsed;
            }
            else
            {
                EffectiveVisibility = Visibility.Visible;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public event EventHandler Tapped;

        public BindableApplicationBarItemBase()
        {
            WireUpCommand();

            // Subscribe to visibility changes so we can raise our VisibilityChanged event
            //
            Binding visibilityBinding = new Binding("Visibility");
            visibilityBinding.Source = this;
            DependencyProperty visiblityProperty = DependencyProperty.RegisterAttached(
                "VisibilityProxyProperty",
                typeof(object),
                typeof(BindableApplicationBarItemBase),
                new PropertyMetadata(OnVisibilityChanged));
            SetBinding(visiblityProperty, visibilityBinding);
        }

        protected void OnClick(object sender, EventArgs e)
        {
            if (Command != null)
            {
                Command.Execute(null);
            }
            if (Tapped != null)
            {
                Tapped(this, EventArgs.Empty);
            }
        }

        private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BindableApplicationBarItemBase me = (BindableApplicationBarItemBase)d;
            me.UpdateText((string)e.NewValue);
        }

        private static void OnCommandPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BindableApplicationBarItemBase me = (BindableApplicationBarItemBase)d;
            ICommand oldCommand = (ICommand)e.OldValue;

            if (oldCommand != null)
            {
                oldCommand.CanExecuteChanged -= me.OnCanExecuteChanged;
            }

            me.WireUpCommand();
            me.CalculateEffectiveVisibility();
        }

        private static void OnVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BindableApplicationBarItemBase me = (BindableApplicationBarItemBase)d;
            me.CalculateEffectiveVisibility();
        }

        private static void OnHideWhenDisabledPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BindableApplicationBarItemBase me = (BindableApplicationBarItemBase)d;
            me.CalculateEffectiveVisibility();
        }

        private void WireUpCommand()
        {
            if (Command != null)
            {
                Command.CanExecuteChanged += OnCanExecuteChanged;
                ApplyCommandCanExecute();
            }
        }

        private void OnCanExecuteChanged(object sender, EventArgs e)
        {
            CalculateEffectiveVisibility();
            ApplyCommandCanExecute();
        }

        abstract protected void ApplyCommandCanExecute();
        abstract protected void UpdateText(string p);
    }
}
