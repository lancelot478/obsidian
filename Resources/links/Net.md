---
Aliases: [ "#Net" ]
---
```dataviewjs
let properties = dv.pages(dv.current().file.aliases[0]).sort(b => b.file.mtime,"desc").file
dv.table(
	["Link","Desc"],
	properties.map(l => [l.link,l.path]),
)
```


[深入理解计算机网络（二）：应用层 | Taogen's Blog](https://taogenjia.com/2019/08/21/computer-network-2-application-layer/)

[在网络中狂奔：KCP协议](https://www.zhihu.com/tardis/zm/art/112442341?source_id=1003)

[《优化接口设计的思路》系列：第七篇—接口限流策略 - 掘金](https://juejin.cn/post/7322352551089029147?searchId=202404241425321352FD5D45787E6EC5BB)

[终于有人把正向代理和反向代理解释的明明白白了！-腾讯云开发者社区-腾讯云](https://cloud.tencent.com/developer/article/1418457)

[30张图解： TCP 重传、滑动窗口、流量控制、拥塞控制 - 小林coding - 博客园](https://www.cnblogs.com/xiaolincoding/p/12732052.html)




