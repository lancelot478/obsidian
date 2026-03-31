---
aliases:
---
´```
打包机 172.26.150.177
muffin 123456
```

```note
港澳台正式服 muffin-tw-prod-agent.xdgtw.com:9190
港澳台QA服 172.25.135.48:9190
国服QA 172.26.149.118:9290
港澳台灰度服 muffin-tw-pre-agent.xdgtw.com:9190
越南qa 172.25.135.32:9290
海外qa 172.25.135.32:9190

国服正式服 muffin-sh-prod-agent.xdgtw.cn:9190
国服灰度服 muffin-sh-pre-agent.xdgtw.cn:9190

韩服正式服 muffin-kr-preview-agent.xdgtw.com:9190
日服正式服 muffin-jp-prod-agent.xdgtw.com:9190

韩服灰度服 muffin-kr-pre-agent.xdgtw.com:9190
韩服 obt muffin-kr-prod-agent.xdgtw.com:9190
韩服 cbt muffin-kr-test-agent.xdgtw.com:9190
韩服-QA入口：172.25.135.29:9390

全球正式
muffin-sgp-prod-agent.xdgtw.com:9190
muffin-na-prod-agent.xdgtw.com:9190
全球灰度
muffin-gl-pre-agent.xdgtw.com:9190
muffin-gl-pre2-agent.xdgtw.com:9190

内网  172.25.135.20:8190

MAC  127.0.0.1:9190
MacMini 172.26.151.210
vnc://172.26.149.199/ 
台湾的号		467635257295085569
我的号     609456320273518593  236925
宝宝的号：  619996736911605761 196554
打波利 1 GM ： 36063053
H 10-20/2 * * 1-5

```

```note
mumu模拟器日志 adb connect 127.0.0.1:7555

```

```
打波利1
	正式服
		38520560 自己账号
		36063053 白名单账号
```

```
后台账号
heyunqi  
9h0w27uDlkaaKX6EMN9D

苹果沙盒  
账号：mftester002@xindong.com  
密码：Xd202466  
地区：台湾
沙盒账号  
账号：mfcn001@xindong.com  
密码：Xd123456  
地区：中国大陆
Paypal   
帐号：sb-9b7dh7288801@business.example.com  
密码：12345678
MyCard  
账号：ricky@soft-world.com.tw    
密码：sw123456     
支付密码:  123456

```

```ad-tip
title:本地服编译和运行
make zagent zbattle zcenter zchat zdevops zgame zguild zlogin zmonitor zrecharge zteam
make localserver
./scripts/localserver-admin start
```

```ad-tip
title: web-gmtools编译和运行
TOOLS_TARGET=web-gmtools make tools-target
bin/web-gmtools -bolt admin.bolt -config tools/web-gmtools/server.json -public tools/web-gmtools/public -users tools/web-gmtools/users.json
```


  
git submodule foreach git pull


ps -ef | grep redis-server   查看数据库状态

src/redis-server redis.conf   更新数据库地址

make pbproto   生成服务器proto

redis-cli -p 12000 keys z:monthsign\* | xargs redis-cli -p 12000 del
**个人信息**


