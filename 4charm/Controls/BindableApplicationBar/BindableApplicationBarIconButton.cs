//
// Copyright (c) Microsoft Corporation.  All rights reserved.
//
using Microsoft.Phone.Shell;
using System;
using System.Windows;

namespace PhotosApp.Controls
{
    public class BindableApplicationBarIconButton : BindableApplicationBarItemBase
    {
        public static readonly DependencyProperty IconUriProperty = DependencyProperty.Register(
            "IconUri",
            typeof(Uri),
            typeof(BindableApplicationBarIconButton),
            new PropertyMetadata(null, OnIconUriPropertyChanged));

        public Uri IconUri
        {
            get { return (Uri)GetValue(IconUriProperty); }
            set { SetValue(IconUriProperty, value); }
        }

        public ApplicationBarIconButton ApplicationBarIconButton { get; private set; }

        public BindableApplicationBarIconButton() : base()
        {
            ApplicationBarIconButton = new ApplicationBarIconButton();
            ApplicationBarIconButton.Text = Text;
            ApplicationBarIconButton.Click += OnClick;
        }

        private static void OnIconUriPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BindableApplicationBarIconButton me = (BindableApplicationBarIconButton)d;

            //TODO: figure out how to get the XAML parser to convert the string to Uri for us when set by the VSM
            if (e.NewValue is Uri)
            {
                me.ApplicationBarIconButton.IconUri = (Uri)e.NewValue;
            }
            else
            {
                me.ApplicationBarIconButton.IconUri = new Uri(e.NewValue.ToString(), UriKind.RelativeOrAbsolute);
            }
        }

        protected override void ApplyCommandCanExecute()
        {
            ApplicationBarIconButton.IsEnabled = Command.CanExecute(null);
        }

        protected override void UpdateText(string text)
        {
            ApplicationBarIconButton.Text = text;
        }
    }
}
