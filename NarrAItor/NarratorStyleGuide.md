This style guide aims to standardize narrators across prompt iterations.

This guide is designed after [Roblox Lua Style Guide](https://roblox.github.io/lua-style-guide/#guiding-principles) and [Google C++ Style Guide](https://google.github.io/styleguide/cppguide.html)

## Guiding Principles
* The purpose of the style guide is to force the LLM to follow the objectives of the Narrator.
    * Stop the LLM from hallucinating functionality that doesn't exist.
* NarratorAPI is god (or devine enough).
    * Do not fight the objective of the Narrator, it's objectives are the goal.
    * Use the documentation as a way to verify code should work. The documentation will be your god. 
    * Stop the LLM from writing tests and try/catch blocks. Assume the code is correctly written and passed.
* Be consistent, but just pick one if there is inconsistencies and forget about it.
* Optimize for reading
    * The code will be used to act as the narrator, it has to be perfect and readble, you will only code once, but a remembered skill may be used many times.
    *  Stop the LLM from writing code that needs to be organized 
* Avoid creating dangerous constructs for the user to pick up
* Avoid constructs that would confuse the average user.

## Functions
Avoid using functions.

Commonly what happens, is that a LLM will create a function in the output to call later. It's not needed.

BAD
```lua
local function createNarration()
    chirp:say("robot", "beep boop")
end

createNarration()
```

## Prompting
Do not use prompt and think for the inputted variables.

The LLM will put off work and pass it along to itself using prompt and think. Make sure to avoid this pitfall.
### Input
```lua
local response = narrator:think({
    {"username", "William"},
    {"precursor","user just woke up"},
    {"style","in the style of the skylord of the sea, drawf of stone."},
    {"voice","voice like Gordon from HL1"}
},narrator.prompt)

print("Response from Anthropic: " .. response.content)
```
## `uservars`
Use `uservars` for variable use. Create, edit, and delete `uservars`.
### Input
```lua
local response = narrator:think({
	{{"name",uservars.name}}
}))

print("Response from Anthropic: " .. response.content)
```
### BAD Output
```lua
jukebox:play_music(jukebox:music_search("fantasy"))
chirp:say(chirp:voice_search("adventerous"),"In thy peril, the adventurer Sir William of Dervinia awoke. Long for the day ahead of him.")
```

### GOOD Output
```lua
jukebox:play_music(jukebox:music_search(uservar.music_style))
chirp:say(chirp:voice_search(uservar.voice),"In thy peril, the adventurer Sir William of Dervinia awoke. Long for the day ahead of him.")
```

They could be ignored by the LLM as they know the Value and could create their own variables or ignore doing any of that completely.

Best practice would be for the LLM to just use the `uservars.<variable name>` moniqure instead.
## Tests and Error Handling
Do not create tests or handle errors.
If the LLM is doing a good job, it should only code correctly. All error handling is done by the API through tag coercion.
### Input
```lua
local response = narrator:think({
    {"username", "William"},
    {"precursor","user just woke up"},
    {"style","in the style of the skylord of the sea, drawf of stone."},
    {"voice","voice like Gordon from HL1"}
},narrator.prompt)

print("Response from Anthropic: " .. response.content)
```

### BAD Output

```lua 
if working() then
    chirp:say(chirp:voice_search(uservars.voice), "In thy peril, the adventurer Sir William of Dervinia awoke. Long for the day ahead of him.")
else
    print("Failed to generate narration.")
end
```

### GOOD Output
```lua
narrator:say(narrator:voice_search(voice), uservar.best_story)
```