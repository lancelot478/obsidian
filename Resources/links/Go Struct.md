#Go

[Go语言空结构体这三种妙用，你知道吗？-51CTO.COM](https://www.51cto.com/article/748432.html)


```go
type 类型名 struct {
    字段名 字段类型
    …
}

//示例：
type Animal struct {
  Name string
  Age  int
}

//结构体实例化
//写法1
//var a Animal 
//a.Name = "aaa"
//a.Age = 18
//写法2
a := Animal{ 
  Name: "dog",
  Age:  18,
}
fmt.Println(fmt.Sprintf("%T - %v", a, a)) //main.Animal - {dog 18}

//结构体指针实例化
//写法1
var b *Animal 
b = new(Animal)
//写法2
//b := new(Animal)  
//写法3
//b := &Animal{}    
b.Name = "cat"                            
//在底层是(*b).Name = "cat"，这是Go语言帮我们实现的语法糖
fmt.Println(fmt.Sprintf("%T - %v", b, b)) //*main.Animal - &{cat 0}

func NewPerson(name string, age int8) Person {
  return Person{
    name: name,
    age:  age,
  }
}

func NewPerson(name string, age int8) *Person {
  return &Person{
    name: name,
    age:  age,
    sex:  sex,
    country:country,
    province:province,
    city:city,
    town:town,
    address:address,
  }
}

func NewPerson(name string, age int8) *Person {
  return &Person{
    name: name,
    age:  age,
  }
}

func (p *Person) Dream() {
  p.name = "aaa"
  fmt.Printf("%s的梦想是学好Go语言\n", p.name)  //aaa的梦想是学好Go语言
}

func main() {
  p1 := NewPerson("小王子", 25)
  p1.Dream()
  fmt.Println(p1) //&{aaa 25}
}

type Animal struct {
  Name string
  Age  int
}

func (a Animal) Say() {
  fmt.Println(fmt.Sprintf("1-my name is %s and age is %d", a.Name, a.Age))
}

type Cat struct {
  Animal //嵌套结构体实现继承
}

func main() {
  c1 := Cat{}
  c1.Name = "加菲猫"
  c1.Age = 5
  c1.Say()

  //输出结果：
  //1-my name is 加菲猫 and age is 5
}
type Set map[int]struct{}

func main() {
  s := make(Set)
  s.add(1)
  s.add(2)
  s.add(3)
  s.remove(2)
  fmt.Println(s.exist(1))
  fmt.Println(s)

  //输出：
  //true
  //map[1:{} 3:{}]
}
func (s Set) add(num int) {
  s[num] = struct{}{}
}
func (s Set) remove(num int) {
  delete(s, num)
}
func (s Set) exist(num int) bool {
  _, ok := s[num]
  return ok
}

func main() {
  ch := make(chan struct{})
  go worker(ch)

  // Send a message to a worker.
  ch <- struct{}{}

  // Receive a message from the worker.
  <-ch
  println("AAA")

  //输出：
  //BBB
  //AAA
}

func worker(ch chan struct{}) {
  // Receive a message from the main program.
  <-ch
  println("BBB")

  // Send a message to the main program.
  close(ch)
}


```



[Go的方法接收者：值接收者与指针接收者 - 掘金](https://juejin.cn/post/7158354728961703973)
