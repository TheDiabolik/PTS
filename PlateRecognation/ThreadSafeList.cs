using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    [Serializable]
    //[TypeConverter(typeof(MyTypeConverter))]
    public class ThreadSafeList<T> : IList<T>
    {
        private List<T> _list = new List<T>();
        private object _sync = new object();
        protected bool _IsReadOnly;


        public virtual T this[int index]
        {
            get
            {
                lock (_sync)
                {
                    return (T)_list[index];
                }
            }
            set
            {
                lock (_sync)
                {
                    _list[index] = value;
                }
            }
        }



        public void Add(T value)
        {
            lock (_sync)
            {
                _list.Add(value);

            }
        }

        public void AddRange(IEnumerable<T> items)
        {
            lock (_sync)
            {
                //foreach (var item in items)
                _list.AddRange(items);

            }
        }


        public bool IsReadOnly
        {
            get
            {
                lock (_sync)
                {
                    return _IsReadOnly;
                }
            }
        }

        public int Count
        {
            get
            {
                lock (_sync)
                {
                    return _list.Count;
                }
            }
        }

        public bool Contains(T item)
        {
            lock (_sync)
            {
                return _list.Contains(item);
            }
        }

        public T Find(Predicate<T> predicate)
        {
            lock (_sync)
            {
                return _list.Find(predicate);
            }
        }

        public int FindIndex(Predicate<T> predicate)
        {
            lock (_sync)
            {
                return _list.FindIndex(predicate);
            }
        }

        public T FindLast(Predicate<T> predicate)
        {
            lock (_sync)
            {
                return _list.FindLast(predicate);
            }
        }

        public int FindLastIndex(Predicate<T> predicate)
        {
            lock (_sync)
            {
                return _list.FindLastIndex(predicate);
            }
        }




        public ThreadSafeList<T> FindAll(Predicate<T> predicate)
        {
            lock (_sync)
            {
                ThreadSafeList<T> myList = new ThreadSafeList<T>();
                myList.AddRange(_list.FindAll(predicate));
                return myList;
            }
        }



        //public List<T> FindAll(Predicate<T> predicate)
        //{
        //    lock (_sync)
        //    {
        //        return _list.FindAll(predicate);
        //    }
        //}
        public T FirstOrDefault()
        {
            lock (_sync)
            {
                return _list.FirstOrDefault();
            }
        }

        public int RemoveAll(Predicate<T> predicate)
        {
            lock (_sync)
            {
                return _list.RemoveAll(predicate);
            }
        }

        public ThreadSafeList<T> Except(ThreadSafeList<T> collection)
        {
            lock (_sync)
            {
                ThreadSafeList<T> myList = new ThreadSafeList<T>();
                myList.AddRange(_list.Except(collection));

                return myList;
            }
        }


        public bool Remove(T value)
        {
            lock (_sync)
            {
                return _list.Remove(value);
            }
        }


        public void RemoveAt(int index)
        {
            lock (_sync)
            {
                _list.RemoveAt(index);
            }
        }


        public void RemoveRange(int index, int count)
        {
            lock (_sync)
            {
                _list.RemoveRange(index, count);
            }
        }

        public int IndexOf(T item)
        {
            lock (_sync)
            {
                return _list.IndexOf(item);
            }
        }

        public void Insert(int index, T item)
        {
            lock (_sync)
            {
                _list.Insert(index, item);
            }
        }

        public IEnumerable<T> GetRange(int index, int count)
        {
            lock (_sync)
            {
                return _list.GetRange(index, count);
            }
        }



        public void CopyTo(T[] array, int index)
        {
            lock (_sync)
            {
                _list.CopyTo(array, index);
            }
        }

        public void Clear()
        {
            lock (_sync)
            {
                _list.Clear();
            }
        }

        public T Min(Predicate<T> predicate)
        {
            lock (_sync)
            {
                return this.Min(predicate);
            }

        }



        /// <summary>Güvenli enumerate için anlık kopya döner.</summary>
        public T[] Snapshot()
        {
            lock (_sync) return _list.ToArray();
        }

        /// <summary>Uzun işlemler için: snapshot alıp onun üzerinde gezin.</summary>
        public void ForEach(Action<T> action)
        {
            if (action == null) return;
            var snap = Snapshot();
            foreach (var x in snap) action(x);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            lock (this._sync)
            {
                return NewEnumerator();
            }
        }

        private IEnumerator<T> NewEnumerator()
        {
            return new ThreadSafeEnumerator(this);
        }
        //IEnumerator<T> IEnumerable<T>.GetEnumerator()
        //{
        //     instead of returning an usafe enumerator,
        //     we wrap it into our thread-safe class
        //    return new SafeEnumerator<T>(_list.GetEnumerator(), _sync);
        //}

        //public IEnumerator<T> GetEnumerator()
        //{
        //    lock (_sync)
        //        return _list.GetEnumerator();
        //}


        public IEnumerator<T> GetEnumerator()
        {
            // ÖNEMLİ: enumerate her zaman snapshot üzerinden
            return ((IEnumerable<T>)Snapshot()).GetEnumerator();
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            lock (_sync)
                return this.GetEnumerator();
        }


        IEnumerable<T> Where(Predicate<T> predicate)
        {
            lock (_sync)
                return this.Where(predicate);
        }

        //public ThreadSafeList<T> Where(Predicate<Func<T, T>> predicate)
        //{
        //    lock (_sync)
        //    {
        //        ThreadSafeList<T> myList = new ThreadSafeList<T>();
        //        myList.AddRange(_list.Where(predicate<Predicate));
        //        //this.Where(predicate);
        //        return myList;
        //    }

        //}


        //public ThreadSafeList<T> Where(Predicate<Func<T, T, T>> predicate)
        //{
        //    lock (_sync)
        //    {
        //        ThreadSafeList<T> myList = new ThreadSafeList<T>();
        //        myList.AddRange(this.Where(predicate));
        //        //this.Where(predicate);
        //        return myList;
        //    }

        //}

        //public ThreadSafeList<T> Where(Predicate<T> predicate)
        //{
        //    lock (_sync)
        //    {
        //        ThreadSafeList<T> myList = new ThreadSafeList<T>();
        //        myList.AddRange(_list.Where(predicate));
        //        //this.Where(predicate);
        //        return myList;
        //    }

        //}
        //internal void Remove(global::EDSCloudComputing.SocketCommunication.ClientInfo ci)
        //{
        //    throw new NotImplementedException();
        //}



        private class ThreadSafeEnumerator : IEnumerator<T>
        {
            /// <summary>
            /// Snapshot to enumerate.
            /// </summary>
            private readonly ThreadSafeList<T> collection;

            /// <summary>
            /// Internal enumerator of the snapshot.
            /// </summary>
            private readonly IEnumerator<T> enumerator;

            /// <summary>
            /// Initializes a new instance of the ThreadSafeEnumerator class, creating a snapshot of the given collection.
            /// </summary>
            /// <param name="collection">List to snapshot.</param>
            public ThreadSafeEnumerator(ThreadSafeList<T> collection)
            {
                lock (collection._sync)
                {
                    // Make snapshot of passed in collection.
                    this.collection = new ThreadSafeList<T>();
                    this.collection.AddRange(collection);

                    // Wrapped enumerator.
                    enumerator = this.collection._list.GetEnumerator();
                }
            }






            public void ForEach(Predicate<T> predicate)
            {
                lock (this.collection._sync)
                    this.ForEach(predicate);
            }


            /// <summary>
            /// Gets the element in the collection at the current position of the enumerator.
            /// </summary>
            /// <returns>The element in the collection at the current position of the enumerator.</returns>
            public T Current
            {
                get
                {
                    lock (this.collection._sync)
                    {
                        return enumerator.Current;
                    }
                }
            }

            /// <summary>
            /// Gets the element in the collection at the current position of the enumerator.
            /// </summary>
            /// <returns>The element in the collection at the current position of the enumerator.</returns>
            object IEnumerator.Current
            {
                get
                {
                    lock (this.collection._sync)
                    {
                        return enumerator.Current;
                    }
                }
            }

            /// <summary>
            /// Disposes the underlying enumerator.  Does not set collection or enumerator to null so calls will still
            /// proxy to the disposed instance (and throw the proper exception).
            /// </summary>
            public void Dispose()
            {
                lock (this.collection._sync)
                {
                    enumerator.Dispose();
                }
            }

            /// <summary>
            /// Advances the enumerator to the next element of the collection.
            /// </summary>
            /// <returns>
            /// true if the enumerator was successfully advanced to the next element; false
            /// if the enumerator has passed the end of the collection.
            /// </returns>
            /// <exception cref="System.InvalidOperationException">The collection was modified after the enumerator was created.</exception>
            public bool MoveNext()
            {
                lock (this.collection._sync)
                {
                    return enumerator.MoveNext();
                }
            }

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element
            /// in the collection.
            /// </summary>
            /// <exception cref="System.InvalidOperationException">The collection was modified after the enumerator was created.</exception>
            public void Reset()
            {
                lock (this.collection._sync)
                {
                    enumerator.Reset();
                }
            }
        }
    }
}
