using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    public class ConcurrentObservableCollection<T> : ObservableCollection<T>
    {
        private readonly object _lock = new object();

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            lock (_lock)
            {
                base.OnCollectionChanged(e);
            }
        }

        //public virtual T this[int index]
        //{
        //    get
        //    {
        //        lock (_lock)
        //        {
        //            return (T)_list[index];
        //        }
        //    }
        //    set
        //    {
        //        lock (_lock)
        //        {
        //            _list[index] = value;
        //        }
        //    }
        //}


        public new void Add(T item)
        {
            lock (_lock)
            {
                base.Add(item);
            }
        }

        public new void Remove(T item)
        {
            lock (_lock)
            {
                base.Remove(item);
            }
        }

        //public new void Last()
        //{
        //    lock (_lock)
        //    {
        //        base.Last();
        //    }
        //}

        public new void Clear()
        {
            lock (_lock)
            {
                base.Clear();
            }
        }
        public T ThreadSafeLast()
        {
            lock (_lock)
            {
                if (this.Count == 0)
                    throw new InvalidOperationException("Collection is empty.");

                return this[this.Count - 1];
            }
        }

        public T ThreadSafeLastOrDefault()
        {
            lock (_lock)
            {
                return this.Count > 0 ? this[this.Count - 1] : default(T);
            }
        }
    }
}
