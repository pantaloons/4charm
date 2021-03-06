﻿using _4charm.ViewModels;
using Microsoft.Phone.Controls;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Navigation;

namespace _4charm.Controls
{
    public abstract class BoundPage : PhoneApplicationPage
    {
        private bool _initialized; 
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            Debug.Assert(DataContext is PageViewModelBase);
            PageViewModelBase vm = DataContext as PageViewModelBase;

            if (!_initialized)
            {
                _initialized = true;
                vm.Initialize(NavigationContext.QueryString, e);
                vm.RestoreState(State);
            }
            
            vm.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            Debug.Assert(DataContext is PageViewModelBase);
            PageViewModelBase vm = DataContext as PageViewModelBase;

            vm.SaveState(State);
            vm.OnNavigatedFrom(e);
        }

        protected override void OnRemovedFromJournal(JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);

            Debug.Assert(DataContext is PageViewModelBase);
            PageViewModelBase vm = DataContext as PageViewModelBase;

            vm.OnRemovedFromJournal(e);
        }

        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            base.OnBackKeyPress(e);

            Debug.Assert(DataContext is PageViewModelBase);
            PageViewModelBase vm = DataContext as PageViewModelBase;

            vm.OnBackKeyPress(e);
        }
    }
}
