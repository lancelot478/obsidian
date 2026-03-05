```lua
base64 = {}  
  
local __CODE = {  
    'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P',  
    'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 'a', 'b', 'c', 'd', 'e', 'f',  
    'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v',  
    'w', 'x', 'y', 'z', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '+', '/',  
}  
  
local __ASCII_CODE = {  
    [65]=0,[66]=1,[67]=2,[68]=3,[69]=4,[70]=5,[71]=6,[72]=7,[73]=8,[74]=9,[75]=10,  
    [76]=11,[77]=12,[78]=13,[79]=14,[80]=15,[81]=16,[82]=17,[83]=18,[84]=19,[85]=20,[86]=21,  
    [87]=22,[88]=23,[89]=24,[90]=25,[97]=26,[98]=27,[99]=28,[100]=29,[101]=30,[102]=31,[103]=32,  
    [104]=33,[105]=34,[106]=35,[107]=36,[108]=37,[109]=38,[110]=39,[111]=40,[112]=41,[113]=42,[114]=43,  
    [115]=44,[116]=45,[117]=46,[118]=47,[119]=48,[120]=49,[121]=50,[122]=51,[48]=52,[49]=53,[50]=54,  
    [51]=55,[52]=56,[53]=57,[54]=58,[55]=59,[56]=60,[57]=61,[43]=62,[47]=63,  
}  
  
function base64.encode(text)  
    local len = string.len(text)  
    local residual = len % 3  
    len = len - residual  
    local index = 1  
    local ret = {}  
    for i =1,len,3 do  
        local t1 = string.byte(text,i)  
        local t2 = string.byte(text,i+1)  
        local t3 = string.byte(text,i+2)  
        local num = t1*65536 + t2*256 + t3  
        for j = 3,0,-1 do  
            local tmp = math.floor(num/(2^(j*6)))  
            local pos = tmp%64 + 1  
            ret[index] = __CODE[pos]  
            index = index + 1  
        end  
    end    if residual == 1 then  
        local num = string.byte(text,len+1)  
        num = num * 16  
        local pos = math.floor(num/64)%64 + 1  
        ret[index] = __CODE[pos]  
        pos = num%64+1  
        ret[index+1] = __CODE[pos]  
        ret[index+2] = '='  
        ret[index+3] = '='  
    elseif residual ==2 then  
        local num1 = string.byte(text,len+1)  
        local num2 = string.byte(text,len+2)  
        local num = num1*1024 + num2*4  
        local pos = math.floor(num/4096) % 64 + 1  
        ret[index] = __CODE[pos]  
        pos = math.floor(num/64) %64 + 1  
        ret[index+1] = __CODE[pos]  
        pos = num%64 + 1  
        ret[index+2] = __CODE[pos]  
        ret[index+3] = '='  
    end  
    return table.concat(ret)  
end  
  
function base64.decode(text)  
    local len = string.len(text)  
    if(len%4 ~= 0) then  
        return nil  
    end  
    local residual = 0  
    if string.sub(text,len-1) == '==' then  
        residual = 2  
        len = len - 4  
    elseif string.sub(text,len) == '=' then  
        residual = 1  
        len = len - 4  
    end  
    local index = 1  
    local ret = {}  
    for i = 1,len,4 do  
        local t1 = __ASCII_CODE[string.byte(text,i)]  
        local t2 = __ASCII_CODE[string.byte(text,i+1)]  
        local t3 = __ASCII_CODE[string.byte(text,i+2)]  
        local t4 = __ASCII_CODE[string.byte(text,i+3)]  
  
        --非法 __ASCII_CODE 数字， >= 124        if t1 == nil or t2 == nil or t3 == nil or t4 == nil then  
            return        end        local num = t1*262144 + t2*4096 + t3*64 + t4  
        local t5 = string.char(num%256)  
        num = math.floor(num/256)  
        local t6 = string.char(num%256)  
        num = math.floor(num/256)  
        local t7 = string.char(num%256)  
        ret[index] = t7  
        ret[index+1] = t6  
        ret[index+2] = t5  
        index = index+3  
    end  
    if residual == 1 then  
        local t8 = __ASCII_CODE[string.byte(text,len+1)]  
        local t9 = __ASCII_CODE[string.byte(text,len+2)]  
        local t10 = __ASCII_CODE[string.byte(text,len+3)]  
        local num = t8*4096 + t9*64 + t10  
        local pos = math.floor(num/1024) % 256  
        ret[index] = string.char(pos)  
        pos = math.floor(num/4) % 256  
        ret[index+1] = string.char(pos)  
    elseif residual ==2 then  
        local t8 = __ASCII_CODE[string.byte(text,len+1)]  
        local t9 = __ASCII_CODE[string.byte(text,len+2)]  
        local num = t8*64 + t9  
        local pos = math.floor(num/16)  
        ret[index] = string.char(pos)  
    end  
    return table.concat(ret)  
end
```