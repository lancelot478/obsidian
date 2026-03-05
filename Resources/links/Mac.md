#Tools

```
redis-cli -h host -p port 命令行redis界面
autojump 自动跳转
```

```ad-tip
title:显示隐藏文件夹
defaults write com.apple.finder AppleShowAllFiles YES
du -hd 1 . | sort -hr | tail -n +2
killall finder
Command+Shift+. 
```

```ad-tip
title:GIT diverge from current branch
git fetch origin
git reset --hard <branch-name>
```

```ad-tip
title:找到所有后缀为 .meta 的文件并删除
find . -name "*.meta" -type f -exec rm -rf {} \;
```

```ad-tip
title:显示文件路径
defaults write com.apple.finder AppleShowAllFiles YES
killall finder
```

```ad-tip
title:回退到之前某个版本
git reflog
git reset --hard HEAD@{3}
```

```ad-tip
title:在finder中打开文件所在位置
open -R file-path
```

```ad-tip
title:回退到之前某个版本
git reflog
git reset --hard HEAD@{3}
```

