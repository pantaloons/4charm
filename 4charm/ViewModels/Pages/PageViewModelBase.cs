using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Navigation;

namespace _4charm.ViewModels
{
    public abstract class PageViewModelBase : ViewModelBase
    {
        public virtual void Initialize(IDictionary<string, string> arguments, NavigationEventArgs e)
        {
        }

        public virtual void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        public virtual void OnNavigatedFrom(NavigationEventArgs e)
        {
        }

        public virtual void OnRemovedFromJournal(JournalEntryRemovedEventArgs e)
        {
        }

        public virtual void OnBackKeyPress(CancelEventArgs e)
        {
        }
    }
}
