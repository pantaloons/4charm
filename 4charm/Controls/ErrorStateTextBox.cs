using System.Windows;
using System.Windows.Input;

namespace _4charm.Controls
{
    public class ErrorStateTextBox : SelectionBindableTextBox
    {
        #region IsErrorState DependencyProperty

        public static readonly DependencyProperty IsErrorStateProperty = DependencyProperty.Register(
            "IsErrorState",
            typeof(bool),
            typeof(ErrorStateTextBox),
            new PropertyMetadata(false, OnIsErrorStateChanged));

        public bool IsErrorState
        {
            get { return (bool)GetValue(IsErrorStateProperty); }
            set { SetValue(IsErrorStateProperty, value); }
        }

        private static void OnIsErrorStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as ErrorStateTextBox).IsErrorStateChanged();
        }

        #endregion

        public ErrorStateTextBox()
        {
            DefaultStyleKey = typeof(ErrorStateTextBox);
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
                if (FocusManager.GetFocusedElement() == this)
                {
                    VisualStateManager.GoToState(this, "Focused", true);
                }
                else
                {
                    VisualStateManager.GoToState(this, "Unfocused", true);
                }
            }
        }
    }
}
