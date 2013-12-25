using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;
using _4charm.ViewModels;

namespace _4charm.Models
{
    class DelayLoadingObservableCollection<T> : ObservableCollection<T> where T : class
    {
        private bool _isResolving;
        private int _delay;
        private List<NotifyCollectionChangedEventArgs> _actions;

        private int _flushCount;
        private int _flushLimit;
        private int _flushDelay;
        private int _flushGroupCount;

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

        public DelayLoadingObservableCollection(int delay, bool isPaused, int bulkAfter, int bulkDelay, int bulkCount)
        {
            _delay = delay;
            _actions = new List<NotifyCollectionChangedEventArgs>();
            _isPaused = isPaused;

            _flushCount = 0;
            _flushLimit = bulkAfter;
            _flushDelay = bulkDelay;
            _flushGroupCount = bulkCount;
        }

        public void AddRange(IEnumerable<T> items, int delay = 0)
        {
            foreach (T item in items)
            {
                _actions.Add(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
            }

            if (delay != 0)
            {
                Task.Delay(delay).ContinueWith(task =>
                {
                    ResolveChanges();
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
            else
            {
                ResolveChanges();
            }
        }

        public new void Add(T item)
        {
            _actions.Add(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));

            ResolveChanges();
        }

        public new void Insert(int index, T item)
        {
            _actions.Add(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));

            ResolveChanges();
        }

        public IEnumerable<T> All()
        {
            return Items.Union(_actions.Where(x => x.Action == NotifyCollectionChangedAction.Add).Select(x => (T)x.NewItems[0]));
        }

        public void RemoveAndPending(T item)
        {
            Items.Remove(item);

            _actions = _actions.Where(x => x.Action != NotifyCollectionChangedAction.Add && (T)x.NewItems[0] != item).ToList();
        }

        public new void Move(int oldIndex, int newIndex)
        {
            _actions.Add(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, new object(), newIndex, oldIndex));

            ResolveChanges();
        }

        protected new void RemoveItem(int index)
        {
            _actions.Add(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, new object(), index));

            ResolveChanges();
        }

        public new void RemoveAt(int index)
        {
            _actions.Add(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, new object(), index));

            ResolveChanges();
        }

        protected new void SetItem(int index, T item)
        {
            Debug.Assert(false);
        }

        public new void Clear()
        {
            base.Clear();
            _flushCount = 0;
            _actions.Clear();
        }

        public void Flush()
        {
            while (_actions.Count > 0)
            {
                NotifyCollectionChangedEventArgs args = _actions[0];
                _actions.RemoveAt(0);
                ResolveChange(args);
            }
        }

        public void Flush(int count)
        {
            for (int i = 0; i < count && _actions.Count > 0; i++)
            {
                NotifyCollectionChangedEventArgs args = _actions[0];
                _actions.RemoveAt(0);
                ResolveChange(args);
            }
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

                NotifyCollectionChangedEventArgs args = _actions[0];
                _actions.RemoveAt(0);

                ResolveChange(args);

                if (_flushCount < _flushLimit)
                {
                    await Task.Delay(_delay);
                }
                else
                {
                    if ((_flushCount + _flushLimit) % _flushGroupCount == 0 && _flushDelay > 0)
                    {
                        await Task.Delay(_flushDelay);
                    }
                }

                _flushCount++;
            }
        }

        private void ResolveChange(NotifyCollectionChangedEventArgs args)
        {
            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    base.InsertItem(args.NewStartingIndex >= 0 ? args.NewStartingIndex : Count, (T)args.NewItems[0]);
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
