using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace _4charm.Models
{
    /// <summary>
    /// Observable collection that maintains a sort order.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    class SortedObservableCollection<T> : ObservableCollection<T> where T : IComparable<T>
    {
        public SortedObservableCollection(IEnumerable<T> enumerable)
        {
            foreach (T item in enumerable) InsertItem(0, item);
        }

        /// <summary>
        /// Override the insert method to insert the item in sorted position.
        /// </summary>
        /// <param name="index">The desired insertion index, this is ignored.</param>
        /// <param name="item">The item to insert.</param>
        protected override void InsertItem(int index, T item)
        {
            int pos = FindInsertPos(item);
            if (pos == -1) return; // Duplicate element, do nothing
            base.InsertItem(pos, item);
        }

        /// <summary>
        /// Find the position the item should be inserted
        /// in the collection, to maintain sort order.
        /// </summary>
        /// <param name="item">The item to find the insert position for</param>
        /// <returns>The sorted insert index, or -1 if the item is a duplicate.</returns>
        private int FindInsertPos(T item)
        {
            int lo = 0, hi = Count;
            while (lo <= hi && lo < Count)
            {
                int mid = (lo + hi) / 2;

                int cmp = item.CompareTo(Items[mid]);
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
    }
}
