using System.Collections.Generic;
using System.Threading;

namespace Flagsmith.Caching.Impl
{
    public class Pool<T> where T : class, new()
    {
        private class Acquired
        {
            public string Name { get; set; }
            public T Object { get; set; }
            public int Counter { get; set; }
        }

        private class Pooled : IPooled<T>
        {
            private Pool<T> _parent;
            private LinkedListNode<Acquired> _node;

            public T Value
            {
                get { return _node.Value.Object; }
            }

            public void Dispose()
            {
                var temp = Interlocked.Exchange(ref _node, null);
                if (temp != null)
                {
                    _parent.Release(temp);
                    _parent = null;
                }
            }

            public Pooled(Pool<T> parent, LinkedListNode<Acquired> node)
            {
                _parent = parent;
                _node = node;
            }
        }

        private readonly LinkedList<T> _pool = new LinkedList<T>();
        private readonly LinkedList<Acquired> _acquired = new LinkedList<Acquired>();
        private readonly object _sync = new object();

        public IPooled<T> Get(string name)
        {
            lock (_sync)
            {
                var node = GetAcquired(name);
                node.Value.Counter++;
                return new Pooled(this, node);
            }
        }

        private LinkedListNode<Acquired> GetAcquired(string name)
        {
            for (var found = _acquired.First; found != null; found = found.Next)
            {
                if (found.Value.Name == name)
                    return found;
            }

            var obj = _pool.First?.Value;
            if (obj != null)
                _pool.RemoveFirst();
            else
                obj = new T();

            var node = new LinkedListNode<Acquired>(new Acquired { Name = name, Object = obj });
            _acquired.AddLast(node);
            return node;
        }

        private void Release(LinkedListNode<Acquired> acquired)
        {
            lock (_sync)
            {
                if (--acquired.Value.Counter == 0)
                {
                    _acquired.Remove(acquired);
                    _pool.AddLast(acquired.Value.Object);
                }
            }
        }
    }
}
