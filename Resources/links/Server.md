---
Aliases: [ "#Server" ]
---
```dataviewjs
let properties = dv.pages(dv.current().file.aliases[0]).sort(b => b.file.mtime,"desc").file
dv.table(
	["Link","Desc"],
	properties.map(l => [l.link,l.path]),
)
```



