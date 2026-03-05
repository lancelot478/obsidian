#Muffin

# 说明

数据按照模块划分

**模块数据之间彼此隔离，当数据变化，只影响当前模块**

# 使用

## LuaPlayerPrefs相关接口

原来的LuaPlayerPrefs相关代码和接口不变

此接口默认分配一个模块，**建议大家避免使用此接口**

## 业务模块使用接口

接口基本跟LuaPlayerPrefs一致

`local prefsInstance = PlayerPrefsInstance.new(moduleName, userSettings) prefsInstance:GetString(key, defaultString) prefsInstance:GetInt(key, defaultInt) prefsInstance:Set(key, value) ...`

- `moduleName` 当前的模块名称字符串，必填
    
- `userSettings` 是否是当前用户的数据，可选，默认否
    

两个**不同的模块之间key是彼此独立**的，比如设置模块有个key叫做`enable` ，跟战斗模块的`enable`互不影响，也无法互相访问。

所以只要保证模块Key不冲突，内部数据key可以简短表示。

# 性能优化

## 模块分离

模块之间彼此独立，化整为零

## Key和Value分离

数据的Key是不经常发生变化的，但是值是经常变更的，每次保存到本地，Key一般是不会变化的，但是放在一起会导致序列化的数据量变大。

### 之前的数据

`{ "GamePushEnabled_12":true, "audioEnable":false, "soundEnable":false, "GamePushEnabled_6":true, "quality":2, "soundBalanceEnable":false, "GamePushEnabled_2":true, "GamePushEnabled_8":true, "GamePushEnabled_7":true, "GamePushEnabled_11":true, "GamePushEnabled_1":true, "GamePushEnabled_9":true, "GamePushEnabled_10":true }`

### 新的数据

`{"5":false,"19":true,"1":2,"18":true,"22":true,"17":true,"8":true,"14":true,"15":true,"16":true,"21":true,"11":2,"7":false,"20":true}`

## 数据比对

Dirty标记修改为比对数值，当数据没有发生变化的时候，即使调用了Save接口也不会重复写入数据

大量减少了序列化数据和写入本地的调用次数