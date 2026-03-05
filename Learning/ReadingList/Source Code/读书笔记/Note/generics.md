# record types

# closures: 
`they can use local variables in the context within which they’re created.`
# expression-bodied members
=>
# String handling
`throw new KeyNotFoundException($"No calendar system for ID {id} exists")`
# LINQ
`var offers =
from product in db.Products
where product.SalePrice <= product.Price / 2
orderby product.SalePrice
select new {
product.Id, product.Description,
product.SalePrice, product.Price
}`
# Async 
`async await`

# Generics

## Where to Use

* Collections (they’re just as useful in collections as they ever were)
* Delegates, particularly in LINQ
* Asynchronous code, where a Task<T> is a promise of a future value of type T 
* Nullable value types

## Collections
* Array `string[] names = new string[4];`  自己扩容
* ArrayList `ArrayList names = new ArrayList();` 自动扩容，隐式转换 `InvalidCastException`
* StringCollection `StringCollection names = new StringCollection();` 无需转换，但限制太大
* T `List<string> names = new List<string>();` 动态传递，代码复用,无需转换（大多数时候）。使用时类型严格检查，不易出错。

## CANNOT GENERICS
Methods and nested types can be generic, but all of the following have to be nongeneric:
*  Fields
*  Properties
*  Indexers
*  Constructors
*  Events
*  Finalizers
## Type Inference
 `List<int> firstTwo = CopyAtMost(numbers, 2);` 方法调用可以类型推断
 `Tuple.Create(10, "x", 20)`
## Type Constraints
* Where 
  * new()
  * class
  * IComparable<T>
* Default(T)
* 用具体类型调用Generics时，会对每个调用类型设置不同的fields和constructor
# Nullable Values
* error prone
* precise
* no boxing equivalent
* `GetValueOrDefault`
# LIFTED OPERATORS
* Unary: + ++ - -- ! ~ true false
* Binary:5 + - * / % & | ^ << >>
* Equality: == !=
* Relational: < > <= >=
## Special
* nullInt <= nullInt return false (nullInt == nullInt return true)
## THE AS OPERATOR AND NULLABLE VALUE TYPES
` int? nullable = o as int?;`
`x ?? y ??`
## Simplified Delegate Creation
`button.Click += HandleButtonClick;`
## Anonymous methods
anonymous method as a closure
## Lazy Execution(IEnumerable)
IEnumerable内部实现了一个state machine,记录其状态，用于跳转到上次yield之后的位置。
```
static IEnumerable<int> CreatIterator()
{
    yield return 1;
    dosomething when next call;
}
```
```
using (IEnumerator<string> enumerator = enumerable.GetEnumerator())
```
* enumerable=> book
* enumerator=> bookmark
# Partial types
* class
* method
# Static class
* forbid:  instance methods, properties, events, or constructors