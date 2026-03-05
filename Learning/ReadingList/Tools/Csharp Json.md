#Tools
解析为数组
JsonUtility的FromJson和JsonConvert的DeserializeObject方法都能够用来将字符串解析成对象，用法分别是：

LevelProperty rawArray = JsonConvert.DeserializeObject<LevelProperty>(originString);

LevelProperty rawArray = JsonUtility.FromJson<LevelProperty>(originString);

但JsonUtility不支持将字符串解析为数组，也就是说以下写法是不行的(会报错，让指定转换类型)：

LevelProperty[] rawArray = JsonUtility.FromJson<LevelProperty[]>(originString);

但JsonConvert可以：

LevelProperty[] rawArray = JsonConvert.DeserializeObject<LevelProperty[]>(originString);



**解决后：**

```csharp
  private void Start () {
        string json = "[1, 2, 3]";
        int[] array = new int[] { 1, 2, 3 };
 
        // 数组转Json
        print(JsonUtil.toJson(array));      // 结果：[1,2,3]
        print(JsonUtility.ToJson(array));   // 结果： {}
 
 
        // Json转数组
        print(JsonUtil.fromJson<int[]>(json));      // 结果：System.Int32[]
        print(JsonUtility.FromJson<int[]>(json));   // 错误：ArgumentException: JSON must represent an object type.
    }

```

**解决思路：**

由于转对象没问题，转数组才会产生问题。所以解决思路是：  
转换时遇到数组，在数组外加壳，作为对象转成JSON，再去壳。  
解析时遇到数组，先套壳转成对象，再取对象里的数组。

**JsonUtil类：**

```csharp
using UnityEngine;
 
/// <summary>
/// Json转换工具类
/// <para>解决JsonUtility转换数组失败的问题</para>
/// <para>ZhangYu 2018-05-09</para>
/// </summary>
public static class JsonUtil {
 
    /// <summary> 把对象转换为Json字符串 </summary>
    /// <param name="obj">对象</param>
    public static string toJson<T>(T obj) {
        if (obj == null) return "null";
 
        if (typeof(T).GetInterface("IList") != null) {
            Pack<T> pack = new Pack<T>();
            pack.data = obj;
            string json = JsonUtility.ToJson(pack);
            return json.Substring(8, json.Length - 9);
        }
 
        return JsonUtility.ToJson(obj);
    }
 
    /// <summary> 解析Json </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="json">Json字符串</param>
    public static T fromJson<T>(string json) {
        if (json == "null" && typeof(T).IsClass) return default(T);
 
        if (typeof(T).GetInterface("IList") != null) {
            json = "{\"data\":{data}}".Replace("{data}", json);
            Pack<T> Pack = JsonUtility.FromJson<Pack<T>>(json);
            return Pack.data;
        }
 
        return JsonUtility.FromJson<T>(json);
    }
 
    /// <summary> 内部包装类 </summary>
    private class Pack<T> {
        public T data;
    }
 
}
```

