---
Aliases: [ "#React" ]
---
```dataviewjs
let properties = dv.pages(dv.current().file.aliases[0]).sort(b => b.file.mtime,"desc").file
dv.table(
	["Link"],
	properties.map(l => [l.link]),
)
```
















