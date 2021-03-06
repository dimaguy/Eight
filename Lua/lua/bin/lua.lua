local term = require("term")
local colors = require("colors")
local colours = colors

local tArgs = { ... }
if #tArgs > 0 then
    print("This is an interactive Lua prompt.")
    print("To run a lua program, just type its name.")
    return
end

--local pretty = require "cc.pretty"

local bRunning = true
local tCommandHistory = {}
local tEnv = {
    ["exit"] = setmetatable({}, {
        __tostring = function() return "Call exit() to exit." end,
        __call = function() bRunning = false end,
    }),
    ["_echo"] = function(...)
        return ...
    end,
}
setmetatable(tEnv, { __index = _ENV })

for k, v in pairs(package.loaded) do
    tEnv[k] = v
end

term.setForeground(colours.yellow)
print("Interactive Lua prompt.")
print("Call exit() to exit.")
term.setForeground(colours.white)

while bRunning do
    term.setForeground( colours.yellow )
    write("lua> ")
    term.setForeground( colours.white )

    local s = term.read(nil, tCommandHistory)
    if s:match("%S") and tCommandHistory[#tCommandHistory] ~= s then
        table.insert(tCommandHistory, s)
    end

    local nForcePrint = 0
    local func, e = load(s, "=lua", "t", tEnv)
    local func2 = load("return _echo(" .. s .. ");", "=lua", "t", tEnv)
    if not func then
        if func2 then
            func = func2
            e = nil
            nForcePrint = 1
        end
    else
        if func2 then
            func = func2
        end
    end

    if func then
        local tResults = table.pack(pcall(func))
        if tResults[1] then
            local n = 1
            while n < tResults.n or n <= nForcePrint do
                local value = tResults[n + 1]
                print(tostring(value))
                n = n + 1
            end
        else
            printError(tResults[2])
        end
    else
        printError(e)
    end

end