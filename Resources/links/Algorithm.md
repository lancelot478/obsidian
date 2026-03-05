---
Aliases: [ "#Algorithm" ]
---
```dataviewjs
let properties = dv.pages(dv.current().file.aliases[0]).sort(b => b.file.mtime,"desc").file
dv.table(
	["Link"],
	properties.map(l => [l.link]),
)
```

[GitHub - wangzheng0822/algo: 数据结构和算法必知必会的50个代码实现](https://github.com/wangzheng0822/algo)
