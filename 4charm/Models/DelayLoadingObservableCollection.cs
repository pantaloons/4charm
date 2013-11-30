using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Threading.Tasks;

namespace _4charm.Models
{
    class DelayLoadingObservableCollection<T> : ObservableCollection<T>
    {
        private bool _isResolving;
        private int _delay;
        private LinkedList<NotifyCollectionChangedEventArgs> _actions;

        private bool _isPaused;
        public bool IsPaused
        {
            get { return _isPaused; }
            set
            {
                _isPaused = value;
                ResolveChanges();
            }
        }

        public DelayLoadingObservableCollection(int delay, bool isPaused)
        {
            _delay = delay;
            _actions = new LinkedList<NotifyCollectionChangedEventArgs>();
            _isPaused = isPaused;            
        }

        public void AddRange(IEnumerable<T> items)
        {
            foreach (T item in items)
            {
                _actions.AddLast(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
            }

            ResolveChanges();
        }

        public new void Insert(int index, T item)
        {
            _actions.AddLast(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
        }

        protected override void InsertItem(int index, T item)
        {
            _actions.AddLast(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
        }

        protected override void MoveItem(int oldIndex, int newIndex)
        {
            _actions.AddLast(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, new object(), newIndex, oldIndex));
        }

        protected override void RemoveItem(int index)
        {
            _actions.AddLast(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, new object(), index));
        }

        public new void RemoveAt(int index)
        {
            _actions.AddLast(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, new object(), index));
        }

        protected override void SetItem(int index, T item)
        {
            Debug.Assert(false);
        }

        protected override void ClearItems()
        {
            base.ClearItems();
            _actions.Clear();
        }

        private void ResolveChanges()
        {
            if (_isResolving)
            {
                return;
            }

            _isResolving = true;
            ResolveChangesInternal().ContinueWith(task =>
            {
                _isResolving = false;
            }, TaskContinuationOptions.ExecuteSynchronously);
        }

        private async Task ResolveChangesInternal()
        {
            while (_actions.Count > 0)
            {
                if (IsPaused)
                {
                    break;
                }

                NotifyCollectionChangedEventArgs args = _actions.First.Value;
                _actions.RemoveFirst();

                ResolveChange(args);

                await Task.Delay(_delay);
            }
        }

        private void ResolveChange(NotifyCollectionChangedEventArgs args)
        {
            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    base.InsertItem(base.Count, (T)args.NewItems[0]);
                    break;
                case NotifyCollectionChangedAction.Move:
                    base.MoveItem(args.OldStartingIndex, args.NewStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    base.RemoveItem(args.OldStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Reset:
                    Debug.Assert(false);
                    break;
            }
        }
    }
}
