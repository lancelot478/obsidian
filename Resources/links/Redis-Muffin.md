#Muffin
```
userid - >  roleid
redis-cli -p 12000 keys 'z:roleid:*'
roleid - >  userid
redis-cli -p 12000 keys 'z:userid:*'

redis-cli -p 12000 --eval scripts/redis-friend.lua

redis-cli -p 12000 GET "z:userid:online:100023"
```

