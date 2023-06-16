using System;

namespace MOT.CORE.Utils.Pool
{
    public sealed class PoolObject<T> : IDisposable
    {
        private readonly IPool<T> _pool;

        public PoolObject(T @object, IPool<T> pool)
        {
            Object = @object;
            _pool = pool;
            IsInPool = true;
        }

        public bool IsInPool { get; set; }
        public T Object { get; private set; }

        public void Release()
        {
            Dispose();
        }

        public void Dispose()
        {
            IsInPool = true;
            _pool.Release(this);
        }
    }
}
