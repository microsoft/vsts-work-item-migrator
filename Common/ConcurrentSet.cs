using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Common
{
    public class ConcurrentSet<T> : IEnumerable<T>, IReadOnlyCollection<T>, ISet<T>
    {
        private ConcurrentDictionary<T, object> dictionary;

        public ConcurrentSet() : this(EqualityComparer<T>.Default)
        {
        }

        public ConcurrentSet(IEqualityComparer<T> comparer)
        {
            this.dictionary = new ConcurrentDictionary<T, object>(comparer);
        }

        public ConcurrentSet(IEnumerable<T> source, IEqualityComparer<T> comparer)
        {
            this.dictionary = new ConcurrentDictionary<T, object>(source?.Select((s) => new KeyValuePair<T, object>(s, null)), comparer);
        }

        public int Count => this.dictionary.Count;

        public bool IsReadOnly => false;

        public bool Add(T item)
        {
            return this.dictionary.TryAdd(item, null);
        }

        public void Clear()
        {
            this.dictionary.Clear();
        }

        public bool Contains(T item)
        {
            return this.dictionary.ContainsKey(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public void ExceptWith(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this.dictionary.Keys.GetEnumerator();
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public bool Remove(T item)
        {
            return this.dictionary.TryRemove(item, out _);
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public void UnionWith(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        void ICollection<T>.Add(T item)
        {
            if (!this.dictionary.TryAdd(item, null))
            {
                throw new Exception();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.dictionary.Keys.GetEnumerator();
        }
    }
}