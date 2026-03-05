#Unity 
```ad-tip 
title:tricky

   ```lua
   for _,v in ipairs(data) do
	v = v .. "%"
end
   for k,v in ipairs(data) do
	data[k] = data[k] .. "%"
end
	--data变了

local data = {1,2,3}  
for i = 3,1,-1 do  
    table.remove(data,i)  
end
-- data = {}

local data = {{1}}  
for _,v in ipairs(data) do  
   v[1] = v[1]+ 1  
end
--data = {{2}}
end

if sourceConfirm then  
    sourceConfirm = false  
else  
    sourceConfirm = true  
end  
--调用两次 变了
sourceConfirm = (sourceConfirm == true) and false or true
--调用两次 还是true

do
local a 
end
local a
```

```
SetParent trigger toggle.isOn
```


>[!Quote] 编码库

[[base64]]



