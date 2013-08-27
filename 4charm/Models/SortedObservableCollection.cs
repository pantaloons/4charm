using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace _4charm.Models
{
    /// <summary>
    /// Observable collection that maintains a sort order.
    /// 
    /// Mostly used for the PostsPage to ensure that posts do not get out of order.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    class SortedObservableCollection<T> : ObservableCollection<T> where T : IComparable<T>
    {
        private bool useComparer;
        private IComparer<T> comparer;

        public SortedObservableCollection()
        {
            useComparer = false;
        }

        public SortedObservableCollection(IComparer<T> comparer)
        {
            useComparer = true;
            this.comparer = comparer;
        }

        public SortedObservableCollection(IEnumerable<T> enumerable)
        {
            foreach (T item in enumerable) InsertItem(0, item);
        }

        private int FindInsertPos(T item)
        {
            int lo = 0, hi = Count;
            while (lo <= hi && lo < Count)
            {
                int mid = (lo + hi) / 2;

                int cmp = useComparer ? comparer.Compare(item, Items[mid]) : item.CompareTo(Items[mid]);
                if (cmp < 0)
                {
                    hi = mid - 1;
                }
                else if (cmp > 0)
                {
                    lo = mid + 1;
                }
                else
                {
                    return -1;
                }
            }
            return lo;
        }

        protected override void InsertItem(int index, T item)
        {
            int pos = FindInsertPos(item);
            if (pos == -1) return; // Duplicate element, do nothing
            base.InsertItem(pos, item);
        }

        public void UpdateItem(T item)
        {
            int oldPos = IndexOf(item);
            if (oldPos == -1)
            {
                InsertItem(0, item);
            }
            else
            {
                int pos = FindInsertPos(item);
                if (pos >= 0) // Last post in the thread actually changed
                {
                    base.Move(oldPos, pos);
                }
            }
        }
    }
}
