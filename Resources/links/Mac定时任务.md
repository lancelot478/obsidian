#Tools

~/Library/LaunchAgents

```命令行
launchctl load gxl.customtask.autogitpull.plist
launchctl unload gxl.customtask.autogitpull.plist
launchctl start gxl.customtask.autogitpull
launchctl list | grep gxl.customtask.autogitpull
```

```
<?xml version="1.0" encoding="UTF-8"?>  
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">  
<plist version="1.0">  
    <dict>  
        <key>Label</key>  
        <string>gxl.customtask.autogitpull</string>  
  
        <key>ProgramArguments</key>  
        <array>  
            <string>/Users/gexianglin/aaboli/auto_git_pull.sh</string>  
        </array>  
  
        <key>StartInterval</key>  
        <integer>7200</integer>  
  
        <key>RunAtLoad</key>  
        <true/>  
  
        <key>StandardOutPath</key>  
        <string>/Users/gexianglin/Desktop/autogitpull.log</string>  
  
        <key>StandardErrorPath</key>  
        <string>/Users/gexianglin/Desktop/autogitpull.err</string>  
  
        <key>EnvironmentVariables</key>  
        <dict>  
            <key>PATH</key>  
            <string>/opt/homebrew/bin:/opt/homebrew/sbin:/usr/local/bin:/usr/bin:/bin:/usr/sbin:/sbin</string>  
        </dict>  
    </dict>  
</plist>
```

auto_git_pull.sh 不能放在桌面，文件等特殊文件夹中

```
#!/bin/bash  
  
_localDir="/Users/gexianglin/aaboli/boli_branch"  
_branchName="2023-11-28-Unity2021-3-13"  
  
cd "$_localDir"  
  
/usr/local/bin/git reset --hard HEAD  
/usr/local/bin/git clean -fd .  
/usr/local/bin/git pull origin "$_branchName" || exit
```

