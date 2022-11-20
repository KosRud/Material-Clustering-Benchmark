using static Diagnostics;

public class ObjectPoolMaxAssert<T> : UnityEngine.Pool.IObjectPool<T> where T : class
{
    private readonly UnityEngine.Pool.ObjectPool<T> pool;

    public readonly int maxActive;

    private string exceededNumObjectsMsg;

    public ObjectPoolMaxAssert(System.Func<T> createFunc, int maxActive)
    {
        this.maxActive = maxActive;
        this.pool = new UnityEngine.Pool.ObjectPool<T>(createFunc);
        this.exceededNumObjectsMsg =
            $"Exceeded maximum number of objects in the pool ({typeof(T)}).";
    }

    public T Get()
    {
        T obj = this.pool.Get();
        Assert(this.pool.CountActive <= this.maxActive, exceededNumObjectsMsg);
        return obj;
    }

    public UnityEngine.Pool.PooledObject<T> Get(out T obj)
    {
        UnityEngine.Pool.PooledObject<T> pooledObj = this.pool.Get(out obj);
        Assert(this.pool.CountActive <= this.maxActive, exceededNumObjectsMsg);
        return pooledObj;
    }

    public void Release(T obj)
    {
        this.pool.Release(obj);
    }

    public void Clear()
    {
        this.pool.Clear();
    }

    public int CountInactive => this.pool.CountInactive;
}
