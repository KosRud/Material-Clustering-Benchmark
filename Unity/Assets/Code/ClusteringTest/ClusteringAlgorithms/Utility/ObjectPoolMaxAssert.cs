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

    public T Get() {
      T obj = this.pool.Get();
      UnityEngine.Debug.Assert(this.pool.CountActive <= this.maxActive);
      return obj;
    }

    public UnityEngine.Pool.PooledObject<T> Get(out T obj) {
      UnityEngine.Pool.PooledObject<T> pooledObj = this.pool.Get(out obj);
      UnityEngine.Debug.Assert(this.pool.CountActive <= this.maxActive);
      return pooledObj;
    }

    public void Release(T obj) {
      this.pool.Release(obj);
    }

    public void Clear() {
      this.pool.Clear();
    }

    public int CountInactive => this.pool.CountInactive;
  }
