using System;
using System.Collections.Concurrent;

namespace MOT.CORE.Utils.Pool
{
    public sealed class Pool<T> : IPool<T> where T : IPoolable
    {
        private readonly ConcurrentStack<PoolObject<T>> _poolObjects;

        public Pool(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException("Capacity must be greater than 0.");

            _poolObjects = new ConcurrentStack<PoolObject<T>>();

            for (int i = 0; i < capacity; i++)
                _poolObjects.Push(new PoolObject<T>(Activator.CreateInstance<T>(), this));
        }

        public int Capacity { get; private set; }

        public PoolObject<T> Get()
        {
            if (_poolObjects.TryPop(out PoolObject<T> @object))
            {
                @object.IsInPool = true;
                return @object;
            }

            return new PoolObject<T>(Activator.CreateInstance<T>(), this) { IsInPool = false };
        }

        public void Release(PoolObject<T> @object)
        {
            @object.Object.Reset();
            _poolObjects.Push(@object);
        }
    }
}
