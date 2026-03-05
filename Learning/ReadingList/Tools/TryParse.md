最近在做一个控制台的Demo时，遇到一个简单验证问题，觉得有意思，就分离出来共勉。

具体场景是当需要在控台上修改一个实体的各个属性时，需要对输入进行验证，如果什么都不输入，直接回车，就返回旧的数据。这里有一个问题，因为所有从Console.ReadLine()读进来的都是string类型，当需要bool，DateTime和数值类型时，就需要转换成对应类型，但如果用输的string本身就不能转成对应类型，就会报错。所以在输入后就要对输入的值进行验证。场景中另外一个要求是如果多次输入都无效，就结束操作。

关于验证，首先想到的是正则，它肯定在验证上是无敌的存在，但用在这里，一是杀鸡用牛刀，二是简单类型有很多，不同类型都需要一个正则表达式，所以正则用在这里不太适合。  

其实需求就是判断一个简单类型的string态正常与否，比如bool类型的string态是不是true或false，这样的话用简单类的TryParse方法就能完成，但同样的问题来了，不能每个简单类型都穷举一次，来看这个string值能不能转换成对应的类型。所以就对几个简单类型进行了源码查看，会发现他们都继承了一个接口IParsable<T>，可以利用这个接口，来做一个统一转换验证的方法，具体实现见下面代码里的ReInput<T>()方法。


```Csharp
T? ReInput<T>(T? oldValue, int times) where T : struct, IParsable<T>  
{  
    for (int i = 1; i <= times; i++)  
    {  
        var input = Console.ReadLine();  
        if (input == "")  
        {  
            return oldValue;  
        }  
  
        if (T.TryParse(input, new TestFormat(), out T newValue))  
        {  
            return newValue;  
        }  
    }
}
```

案例本身不复杂，这里有所感悟的是：要对共用的东西抽象，抽象后要做到职责单一，还要做到接口隔离，达到这些所谓的标准后，就给后期使用者，留下了很多扩展方式和想象空间。