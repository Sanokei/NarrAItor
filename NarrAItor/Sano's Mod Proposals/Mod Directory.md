Mod directory system to allow the bots to use each other mod's
```
@Modname moddir
@Version 0.0.1
```
## Methods
Implements Mod Directory from Narrator Bot into Lua.
### bool __try\_add(Table mods)__
try to add to directory. Must have a unique modname. 
#### parameters
##### Table mods
The mods packaged into a table
#### usage
```lua
moddir:add({
    {"<modname>",packed_mod}
})
```
### bool **try\_remove(string modname)**
Try to remove mods from directory.
#### parameters
##### Table mods
The mods packaged into a table
#### usage
```lua
moddir:remove("<modname>")
```

### int **remove\_all()**
Removes all mods from directory.
#### usage
```lua
moddir:removeall()
```
### bool **has(string modname)**
Check the mods directory for a mod
#### parameters
##### Table mods
The mods packaged into a table
#### usage
```lua
if(moddir:has("<modname>")) do
	print("Mod exists")
	end
```