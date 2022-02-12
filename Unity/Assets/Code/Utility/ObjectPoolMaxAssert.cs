public class ObjectPoolMaxAssert<T> : UnityEngine.Pool.IObjectPool<T>
where T : class {
    private readonly UnityEngine.Pool.ObjectPool<T> pool;

    public readonly int maxActive;

    public ObjectPoolMaxAssert(
        System.Func<T> createFunc,
        int maxActive
    ) {
        this.maxActive = maxActive;
        this.pool = new UnityEngine.Pool.ObjectPool<T>(createFunc);
    }

    T UnityEngine.Pool.IObjectPool<T>.Get() {
        T obj = this.pool.Get();
        UnityEngine.Debug.Assert(this.pool.CountActive <= this.maxActive);
        return obj;
    }

    UnityEngine.Pool.PooledObject<T> UnityEngine.Pool.IObjectPool<T>.Get(out T obj) {
        UnityEngine.Pool.PooledObject<T> pooledObj = this.pool.Get(out obj);
        UnityEngine.Debug.Assert(this.pool.CountActive <= this.maxActive);
        return pooledObj;
    }

    void UnityEngine.Pool.IObjectPool<T>.Release(T obj) {
        this.pool.Release(obj);
    }

    void UnityEngine.Pool.IObjectPool<T>.Clear() {
        this.pool.Clear();
    }

    int UnityEngine.Pool.IObjectPool<T>.CountInactive => this.pool.CountInactive;
}
