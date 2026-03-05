#Muffin
```
打印redis日志
/Users/gexianglin/zserver/internal/db/repo/redis.go
initMetaRedisDB(poolConf, c.Metas)  
defaultMetaRedisDB.SetSlowLogTime(1)
```
agent 和 game 服的连接
```go
//打印堆栈
zaplog.S.Errorf("%s", zeroutil.GetStack())

internal/app/game/service.go
	 //通过 remoteAgentBackendService 处理agent服的连接
	NewBackendService
	//将 remoteAgentBackendService  传入  AgentManager
    s.sessmgr = tunnel.NewAgentManager(id, service, nil，&remoteAgentBackendService{id: id, router: router})

remoteAgentBackendService 
		//在收到backend发来的请求后，
		ServeConn //创建 AgentSession会话
		
internal/session/game/session.go
		//从 AgentSession拿到 UserSession
		SessionManager.AddSession
		

//game服 通过中间件 主动连接 agent服
pkg/tunnel/session_agent.go 
	// 通过 discovery 从 common.toml 读到本地配置的 agent addr
	AgentManager
	serveAgent 主动连接 agent backend 服

//agent服 接受 game 服的连接
internal/session/agent/backend.go

	//拿到 connId
	s.frontendManager.GetSessionID()
	
	ServeBackend
	//第一步 WaitRegister on_register do_register_ack
	//第二步 开启心跳检测 CheckPing
	//第三步 OnMessage 发给 frontend
			//谁发过来的发回去 
				writeFrontend 
			//广播 
				broadcastFrontends
			
//agent服 frontend 和 backend 的桥梁
internal/session/agent/service.go
	// AgentService持有  
		backendManager
		
//agent服	
internal/session/agent/frontend.go
	//处理 backend 发来的服务器的返回消息
	go s.waitResponse()  
	//处理客户端的连接
	for {
	    inRequest, err := s.ReadPacket()
    }
    
```

战斗编辑器

```go
pbproto.RegisterBattleEditorServiceServer(grpcSrv, battle.NewBattleEditorGPRCService())
```


```go
func (lru *LRUCache) SetIfAbsent(  
    key string, value interface{}) (interface{}, bool) {  
    now := time.Now()  
    lru.mu.Lock()  
    if element := lru.table[key]; element != nil {  
       // check whether it's expired  
       e := element.Value.(*entry)  
       if !e.expired(now) {  
          e.ttl = lru.ttl  
          e.assessTime = now          lru.list.MoveToFront(element)  
          lru.mu.Unlock()  
          return e.value, false  
       }  
    }    if !lru.replaceOldItem(now, key, value, lru.ttl) {  
       lru.addNew(now, key, value, lru.ttl)  
    }    lru.mu.Unlock()  
    return value, true  
}
func (lru *LRUCache) replaceOldItem(now time.Time, key string, value interface{}, ttl time.Duration) bool {  
    element := lru.table[key]  
    // if existed, just replace its value.  
    if element != nil {  
       e := element.Value.(*entry)  
       e.value = value       e.ttl = ttl       e.assessTime = now       lru.list.MoveToFront(element)  
       return true  
    }  
    // replace expired item or spare one.  
    element = lru.list.Back()  
    if element == nil {  
       return false  
    }  
    e := element.Value.(*entry) 
    //如果容量不够或者已过期 都要删除
    if lru.size < lru.capacity && !e.expired(now) {  
       return false  
    }  
    delete(lru.table, e.key)  
    e.key = key    e.value = value    e.ttl = ttl    e.assessTime = now    lru.table[key] = element  
    lru.list.MoveToFront(element)  
    return true  
}
func (e *entry) expired(t time.Time) bool {  
    return (e.ttl > 0 && t.Sub(e.assessTime) >= e.ttl)  
}
```

```go
//防止并发
func (t *BattleOperationTeamInstance) setLoop() bool {  
    t.lock.Lock()  
    defer t.lock.Unlock()  
  
    isLoop := t.isLoop  
    if !isLoop {  
       t.isLoop = true  
    }  
    return isLoop  
}
```



