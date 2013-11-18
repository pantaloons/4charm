using System.Windows;
using System.Windows.Controls;

namespace _4charm.Controls
{
    public class ErrorStateButton : Button
    {
        #region IsErrorState DependencyProperty

        public static readonly DependencyProperty IsErrorStateProperty = DependencyProperty.Register(
            "IsErrorState",
            typeof(bool),
            typeof(ErrorStateButton),
            new PropertyMetadata(false, OnIsErrorStateChanged));

        public bool IsErrorState
        {
            get { return (bool)GetValue(IsErrorStateProperty); }
            set { SetValue(IsErrorStateProperty, value); }
        }

        private static void OnIsErrorStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as ErrorStateButton).IsErrorStateChanged();
        }

        #endregion

        public ErrorStateButton()
        {
            DefaultStyleKey = typeof(ErrorStateButton);
        }

        private void IsErrorStateChanged()
        {
            if (IsErrorState)
            {
                VisualStateManager.GoToState(this, "Errored", true);
            }
            else
            {
                VisualStateManager.GoToState(this, "Unerrored", true);
            }
        }
    }
}
