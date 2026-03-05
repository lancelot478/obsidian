---
aliases:
---
#Web

```javascript
arr.map(parseInt). //这样会出错，因为会变成下面这样
arr.map(function(val,index){
    return parseInt(val,index)
})

//这样是对的
arr.map(function(val){
	return parseInt(val,10)
})


//去除重复字符串
function removeDumplicatedStr(str){
	let result = Array.prototype.filter.call(str,function(char,index,arr){
		return arr.indexOf(char) == index;
	})
	return result.join('');
}

//交互 a,b
var a = 'a'
var b = 'b'
a = [b,b=a][0] //方案1
a = [b][b=a,0] //方案2


//new
new Cat(name)

function New(){
	var obj = {};
	obj.__proto__ = Cat.prototype;//核心代码，用于继承
	var res = Cat.apply(obj,arguments);
	return typeof res === 'object' ? res :obj
}
New('catname')

//这两个等价，都是修改Cat 的this 到实例化后的obj
```