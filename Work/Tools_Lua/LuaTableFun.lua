local _ENV = Boli2Env

local type = type
local pairs = pairs
local tostring = tostring
local string = string

local function table_print (tt, indent, done)
    done = done or {}
    indent = indent or 0
    if type(tt) == "table" then
        local sb = {}
        for key, value in pairs (tt) do
            table.insert(sb, string.rep (" ", indent)) -- indent it
            if type (value) == "table" and not done [value] then
                done [value] = true
                table.insert(sb, key .. " = {\n");
                table.insert(sb, table_print (value, indent + 2, done))
                table.insert(sb, string.rep (" ", indent)) -- indent it
                table.insert(sb, "}\n");
            elseif "number" == type(key) then
                table.insert(sb, string.format("\"%s\"\n", tostring(value)))
            else
                table.insert(sb, string.format(
                        "%s = \"%s\"\n", tostring (key), tostring(value)))
            end
        end
        return table.concat(sb)
    else
        return tt .. "\n"
    end
end

local function to_string( tbl )
    if  "nil"       == type( tbl ) then
        return tostring(nil)
    elseif  "table" == type( tbl ) then
        return table_print(tbl)
    elseif  "string" == type( tbl ) then
        return tbl
    else
        return tostring(tbl)
    end
end

function sprint_table(root)
    return to_string(root)
end

-- declare local variables
--// exportstring(string)
--// returns a "Lua" portable version of the string
local function exportstring(s)
    return string.format("%q", s)
end

--// The Save Function
function save_table(tbl, filename)
    local charS, charE = "   ", "\n"
    local file, err = io.open(filename, "wb")
    if err then return err end

    -- initiate variables for save procedure
    local tables, lookup = {tbl}, {[tbl] = 1}
    file:write("return {"..charE)

    for idx, t in ipairs(tables) do
        file:write("-- Table: {"..idx.."}"..charE)
        file:write("{"..charE)
        local thandled = {}

        for i, v in ipairs(t) do
            thandled[i] = true
            local stype = type(v)
            -- only handle value
            if stype == "table" then
                if not lookup[v] then
                    table.insert(tables, v)
                    lookup[v] = #tables
                end
                file:write(charS.."{"..lookup[v] .. "},"..charE)
            elseif stype == "string" then
                file:write(charS..exportstring(v) .. ","..charE)
            elseif stype == "number" then
                file:write(charS..tostring(v) .. ","..charE)
            end
        end

        for i, v in pairs(t) do
            -- escape handled values
            if (not thandled[i]) then

                local str = ""
                local stype = type(i)
                -- handle index
                if stype == "table" then
                    if not lookup[i] then
                        table.insert(tables, i)
                        lookup[i] = #tables
                    end
                    str = charS.."[{"..lookup[i] .. "}]="
                elseif stype == "string" then
                    str = charS.."["..exportstring(i) .. "]="
                elseif stype == "number" then
                    str = charS.."["..tostring(i) .. "]="
                end

                if str ~= "" then
                    stype = type(v)
                    -- handle value
                    if stype == "table" then
                        if not lookup[v] then
                            table.insert(tables, v)
                            lookup[v] = #tables
                        end
                        file:write(str.."{"..lookup[v] .. "},"..charE)
                    elseif stype == "string" then
                        file:write(str..exportstring(v) .. ","..charE)
                    elseif stype == "number" then
                        file:write(str..tostring(v) .. ","..charE)
                    end
                end
            end
        end
        file:write("},"..charE)
    end
    file:write("}")
    file:close()
end