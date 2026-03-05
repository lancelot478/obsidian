## 异步编程难点

### 1.异常难处理
异步传递回异常供调用者使用
![[Pasted image 20250225104756.png]]

### 2.函数嵌套过深

![[Pasted image 20250225105025.png]]

### 3.阻塞代码

node缺少 sleep(1000) 这样的线程沉睡

只能使用 setInterval() 和 setTimeout()

### 4.多线程编程

Web Workers 多线程编程

## 解决方案
### 1.事件发布订阅模式
[Events | Node.js v23.8.0 Documentation](http://nodejs.org/docs/latest/api/events.html)
不存在事件冒泡，也不存在preventDefault()、stopPropagation()和stopImmediatePropagation()
#### eventProxy

```javascript
//自定义事件
var events = require('events')
function CustomClass(){
	events.EventEmitter.call(this)
}
util.inherit(CustomClass,events.EventEmitter)
```

```javascript 
//事件队列解决缓存雪崩问题
var proxy = new events.EventEmitter();￼
​​​​​​​​var status = "ready";￼
​​​​​​​​var select = function (callback) {￼
	proxy.once("selected", callback);￼ ​​​​​​​​  
	if (status === "ready") {￼ ​​​​​​​​    
		status = "pending";￼ ​​​​​​​​    
		db.select("SQL", function (results) {￼ ​​​​​​​​      
			proxy.emit("selected", results);￼ ​​​​​​​​      
			status = "ready";￼ ​​​​​​​​    
		});￼ ​​​​​​​​  
	}￼ ​​​​​​​​
};​​
```

```javascript 
//多对一 达到多次后事件触发
var after = function(times,callback){
	var count =0,result = {}
	return function(key,value){
		count++
		result[key] = value
		if(count == times){
			callback(result)
		}
	}
}
```
### 2.Promise/Deferred模式

Q  Promise.then流程链

### 3.流程控制库
中间件，尾触发，面向切片编程
非模式化
#### 相关的库：
app.connect()
async流程模块
step
wind

## 内存泄漏
### 缓存
闭包，全局变量，临时变量引用
模块加速会导致本地缓存

使用Redis做缓存

### 工具
node-heapdump

## Buffer
### slab内存分布策略
utf-8编码 中文截断 

### HTTP 在 TCP 上的封装解决了 Web 通信的三大核心问题：

1. **语义明确化**：通过结构化报文描述操作意图。
2. **功能扩展性**：通过头部、状态码支持缓存、安全、会话等高级特性。
3. **性能优化**：持久连接、压缩、分块传输提升效率。

### Socket

在所有的WebSocket服务器端实现中，没有比Node更贴近WebSocket的使用方式了。它们的共性有以下内容。
❑ 基于事件的编程接口。
❑ 基于JavaScript，以封装良好的WebSocket实现，API与客户端可以高度相似。

socket.io    ws 

### 网络安全

crypto tls https 
### Cookie Session

xss攻击，session 用私钥加密

### 网页缓存
base64编码

bigpipe

为了解决进程复制中的浪费问题，多线程被引入服务模型，让一个线程服务一个请求。线程相对进程的开销要小许多，并且线程之间可以共享数据，内存浪费的问题可以得到解决，并且利用线程池可以减少创建和销毁线程的开销。但是多线程所面临的并发问题只能说比多进程略好，因为每个线程都拥有自己独立的堆栈，这个堆栈都需要占用一定的内存空间。另外，由于一个CPU核心在一个时刻只能做一件事情，操作系统只能通过将CPU切分为时间片的方法，让线程可以较为均匀地使用CPU资源，但是操作系统内核在切换线程的同时也要切换线程的上下文，当线程数量过多时，时间将会被耗用在上下文切换中。所以在大并发量时，多线程结构还是无法做到强大的伸缩性。

### node:
通过消息传递内容，而不是共享或直接操作相关资源，这是较为轻量和无依赖的做法。

go:
通过通信来共享数据，而不是通过共享数据来通信。

mocha
