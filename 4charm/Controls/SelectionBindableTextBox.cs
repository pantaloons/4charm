using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace _4charm.Controls
{
    public class SelectionBindableTextBox : TextBox
    {
        #region UpdateSourceOnTextChanged DependencyProperty

        public static readonly DependencyProperty UpdateSourceOnTextChangedProperty = DependencyProperty.Register(
            "UpdateSourceOnTextChanged",
            typeof(bool),
            typeof(SelectionBindableTextBox),
            new PropertyMetadata(false, OnUpdateSourceOnTextChangedChanged));

        public bool UpdateSourceOnTextChanged
        {
            get { return (bool)GetValue(UpdateSourceOnTextChangedProperty); }
            set { SetValue(UpdateSourceOnTextChangedProperty, value); }
        }

        private static void OnUpdateSourceOnTextChangedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as SelectionBindableTextBox).UpdateSourceOnTextChangedChanged();
        }

        #endregion

        #region SelectionStart DependencyProperty

        public static readonly DependencyProperty BindableSelectionStartProperty = DependencyProperty.Register(
            "BindableSelectionStart",
            typeof(int),
            typeof(SelectionBindableTextBox),
            new PropertyMetadata(0, OnBindableSelectionStartChanged));

        public int BindableSelectionStart
        {
            get { return (int)GetValue(BindableSelectionStartProperty); }
            set { SetValue(BindableSelectionStartProperty, value); }
        }

        private static void OnBindableSelectionStartChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as SelectionBindableTextBox).BindableSelectionStartChanged();
        }

        #endregion

        private bool _isUpdating;

        public SelectionBindableTextBox()
        {
            SelectionChanged += SelectionBindableTextBox_SelectionChanged;
        }

        private void SelectionBindableTextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            _isUpdating = true;
            BindableSelectionStart = SelectionStart;
            _isUpdating = false;
        }

        private void BindableSelectionStartChanged()
        {
            if (!_isUpdating)
            {
                SelectionStart = BindableSelectionStart;
            }
        }

        private void UpdateSourceOnTextChangedChanged()
        {
            if (UpdateSourceOnTextChanged)
            {
                TextChanged += SelectionBindableTextBox_TextChanged;
            }
            else
            {
                TextChanged -= SelectionBindableTextBox_TextChanged;
            }
        }

        private void SelectionBindableTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            GetBindingExpression(TextBox.TextProperty).UpdateSource();
        }
    }
}
