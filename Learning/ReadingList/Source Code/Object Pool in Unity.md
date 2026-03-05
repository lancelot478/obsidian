```Csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.UI
{
    internal static class ListPool<T>
    {
        // Object pool to avoid allocations.
        private static readonly ObjectPool<List<T>> s_ListPool = new ObjectPool<List<T>>(null, Clear);
        static void Clear(List<T> l) { l.Clear(); }

        public static List<T> Get()
        {
            return s_ListPool.Get();
        }

        public static void Release(List<T> toRelease)
        {
            s_ListPool.Release(toRelease);
        }
    }
}
```

```Csharp
public class CollectionPool<TCollection, TItem> where TCollection : class, ICollection<TItem>, new()  
{  
  internal static readonly ObjectPool<TCollection> s_Pool = 
  new ObjectPool<TCollection>((Func<TCollection>) 
  (() => new TCollection()), actionOnRelease: (Action<TCollection>) (l => l.Clear()));  
  
  public static TCollection Get() => 
  CollectionPool<TCollection, TItem>.s_Pool.Get();  
  
  public static PooledObject<TCollection> Get(out TCollection value) => 
  CollectionPool<TCollection, TItem>.s_Pool.Get(out value);  
  
  public static void Release(TCollection toRelease) => 
  CollectionPool<TCollection, TItem>.s_Pool.Release(toRelease);  
}
