
|   |   |   |
|---|---|---|
|`file.name`|Text|The file name as seen in Obsidians sidebar.|
|`file.folder`|Text|The path of the folder this file belongs to.|
|`file.path`|Text|The full file path, including the files name.|
|`file.ext`|Text|The extension of the file type; generally `md`.|
|`file.link`|Link|A link to the file.|
|`file.size`|Number|The size (in bytes) of the file.|
|`file.ctime`|Date with Time|The date that the file was created.|
|`file.cday`|Date|The date that the file was created.|
|`file.mtime`|Date with Time|The date that the file was last modified.|
|`file.mday`|Date|The date that the file was last modified.|
|`file.tags`|List|A list of all unique tags in the note. Subtags are broken down by each level, so `#Tag/1/A` will be stored in the list as `[#Tag, #Tag/1, #Tag/1/A]`.|
|`file.etags`|List|A list of all explicit tags in the note; unlike `file.tags`, does not break subtags down, i.e. `[#Tag/1/A]`|
|`file.inlinks`|List|A list of all incoming links to this file, meaning all files that contain a link to this file.|
|`file.outlinks`|List|A list of all outgoing links from this file, meaning all links the file contains.|
|`file.aliases`|List|A list of all aliases for the note as defined via the [YAML frontmatter](https://help.obsidian.md/How+to/Add+aliases+to+note).|
|`file.tasks`|List|A list of all tasks (I.e., `\| [ ] some task`) in this file.|
|`file.lists`|List|A list of all list elements in the file (including tasks); these elements are effectively tasks and can be rendered in task views.|
|`file.frontmatter`|List|Contains the raw values of all frontmatter in form of `key \| value` text values; mainly useful for checking raw frontmatter values or for dynamically listing frontmatter keys.|
|`file.day`|Date|Only available if the file has a date inside its file name (of form `yyyy-mm-dd` or `yyyymmdd`), or has a `Date` field/inline field.|
|`file.starred`|Boolean|If this file has been bookmarked via the Obsidian Core Plugin "Bookmarks".|



[玩转 Obsidian 08：利用 Dataview 打造自动化 HomePage - 少数派](https://sspai.com/post/73958)

`dv.pages` 很明显是 JavaScript 语法，它的用法如下：

- `dv.pages()` => 「文件仓库」中所有笔记，以下默认都是在「文件仓库」中查询。
- `dv.pages("#books")` => 所有标签是 `books` 的笔记。
- `dv.pages('"folder"')` => 所有在名为 `folder` 的文件夹下的笔记。
- `dv.pages("#yes or -#no")` => 所有包含 `yes` 但是不包含 `no`的笔记。
- `dv.pages('"folder" or #tag')` => 所有在 `folder`文件夹下的笔记「或者」标签包含 `tag` 的笔记。


# Today created file
- List FROM "" WHERE file.cday = date("<%tp.date.now("YYYY-MM-DD")%>") SORT file.ctime asc


```dataviewjs
let pages = dv.pages('"Learning"').sort(b => b.file.mtime,"desc")
let inFileTag = pages.file.lists
	.filter(l => l.tags.includes(dv.current().file.aliases[0]))
dv.table(
	["InfileTag", "Link"],
	inFileTag.map(l => [l.name, l.link]),
)
```
# Recent Read
LIST WHERE file.mtime >= date(today) - dur(10 day) sort file.mtime desc limit (5)

