using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace _4charm.ViewModels
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public void Navigate(Uri uri)
        {
            ((Application.Current.RootVisual as PhoneApplicationFrame).Content as PhoneApplicationPage).NavigationService.Navigate(uri);
        }

        public void GoBack()
        {
            ((Application.Current.RootVisual as PhoneApplicationFrame).Content as PhoneApplicationPage).NavigationService.GoBack();
        }

        private Dictionary<string, object> _properties = new Dictionary<string, object>();
        public T GetProperty<T>([CallerMemberName] string propertyName = null)
        {
            if (!_properties.ContainsKey(propertyName)) return default(T);

            return (T)_properties[propertyName];
        }

        public void SetProperty(object value, [CallerMemberName] string propertyName = null)
        {
            if (_properties.ContainsKey(propertyName) && _properties[propertyName] == value) return;

            _properties[propertyName] = value;
            NotifyPropertyChanged(propertyName);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
