using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;
using _4charm.ViewModels;
using System;

namespace _4charm.Models
{
    class DelayLoadingFilteredObservableCollection<T> : ObservableCollection<T> where T : class
    {
        private bool _isResolving;
        private int _delay;
        private List<NotifyCollectionChangedEventArgs> _actions;

        private int _flushCount;
        private int _flushLimit;
        private int _flushDelay;
        private int _flushGroupCount;

        private ObservableCollection<T> _originalList;
        private Predicate<T> _filter;

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

        public DelayLoadingFilteredObservableCollection(int delay, bool isPaused, int bulkAfter, int bulkDelay, int bulkCount)
        {
            _delay = delay;
            _actions = new List<NotifyCollectionChangedEventArgs>();
            _isPaused = isPaused;

            _flushCount = 0;
            _flushLimit = bulkAfter;
            _flushDelay = bulkDelay;
            _flushGroupCount = bulkCount;

            _originalList = new ObservableCollection<T>();
        }

        public void ApplyFilter(Predicate<T> filter)
        {
            _filter = filter;
            ResolveFilter();
        }

        public void RemoveFilter()
        {
            _filter = null;
            ResolveFilter();
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
            base.Remove(item);

            _actions = _actions.Where(x => x.Action != NotifyCollectionChangedAction.Add && (T)x.NewItems[0] != item).ToList();
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
            _originalList.Clear();
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

                if (ResolveChange(args))
                {
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
        }
        
        private void ResolveFilter()
        {
            Flush();

            if (_filter == null)
            {
                for (int i = 0; i < _originalList.Count; i++)
                {
                    if (i > Count || base.Items[i] != _originalList[i])
                    {
                        base.InsertItem(i, _originalList[i]);
                    }
                }
            }
            else
            {
                int filteredIndex = 0;
                for (int i = 0; i < _originalList.Count; i++)
                {
                    if (!_filter(_originalList[i]) && Count > filteredIndex && base.Items[filteredIndex] == _originalList[i])
                    {
                        base.RemoveAt(filteredIndex);
                    }
                    else if (_filter(_originalList[i]) && (Count <= filteredIndex || base.Items[filteredIndex] != _originalList[i]))
                    {
                        base.InsertItem(filteredIndex, _originalList[i]);
                    }

                    Debug.Assert((_filter(_originalList[i]) && base.Items.Contains(_originalList[i]))
                        || (!_filter(_originalList[i]) && !base.Items.Contains(_originalList[i])));

                    if (_filter(_originalList[i]))
                    {
                        filteredIndex++;
                    }
                }
            }
        }

        private bool ResolveChange(NotifyCollectionChangedEventArgs args)
        {
            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Debug.Assert(args.NewStartingIndex == -1);

                    _originalList.Insert(args.NewStartingIndex >= 0 ? args.NewStartingIndex : _originalList.Count, (T)args.NewItems[0]);
                    if (_filter == null || _filter((T)args.NewItems[0]))
                    {
                        base.InsertItem(args.NewStartingIndex >= 0 ? args.NewStartingIndex : Count, (T)args.NewItems[0]);
                        return true;
                    }
                    return false;
                case NotifyCollectionChangedAction.Remove:
                    _originalList.RemoveAt(args.OldStartingIndex);
                    int index = base.IndexOf((T)args.OldItems[0]);
                    if (index >= 0)
                    {
                        base.RemoveAt(index);
                        return true;
                    }
                    return false;
                case NotifyCollectionChangedAction.Move:
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Reset:
                    Debug.Assert(false);
                    break;
            }

            // Base collection always resolves change
            return true;
        }
    }
}
