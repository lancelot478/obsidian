local _ENV = Boli2Env

---@class GameObjectPool
GameObjectPool = class("GameObjectPool")

function GameObjectPool:ctor(_poolName, _poolObj, _poolPrefab, _buildInNum)
    -- public 
    self.poolName = _poolName
    self.poolTransform = _poolObj.transform
    self.poolPrefab = _poolPrefab
    self.initNum = _buildInNum
    -- private
    self.pool = {}
    self.poolSize = 0
    self:Init()
    return self
end

function GameObjectPool:Instantiate()
    local obj = GlobalFun.Instantiate(self.poolPrefab, self.poolTransform)
    return obj
end

function GameObjectPool:Init()
    if self.initNum then
        for i=1,self.initNum do
            local obj = self:Instantiate()
            self:Cycle(obj.transform)
        end
    end
end

---@return UnityEngine.GameObject
function GameObjectPool:Get()
    local cell
    if self.pool ~= nil then
        if self.poolSize > 0 then
            cell = self.pool[self.poolSize]
            self.pool[self.poolSize] = nil
            self.poolSize = self.poolSize - 1
        else
            cell = self:Instantiate()
        end
    end
    return cell
end

-- @ param type should be "Transform" !!
---@param objTran UnityEngine.Transform
function GameObjectPool:Cycle(objTran)
    if objTran and self.pool and self.poolTransform then
        GlobalFun.SetParent(objTran, self.poolTransform, false)
        --table.insert(self.pool, objTran.gameObject)
        local newPoolSize = self.poolSize + 1
        self.pool[newPoolSize] = objTran.gameObject
        self.poolSize = newPoolSize
    end
end

function GameObjectPool:DestroyAll()
    for k,v in pairs(self.pool) do
        GlobalFun.DestroyObj(v, 0.0)
    end
    self.pool = {}
    self.poolSize = 0
end
