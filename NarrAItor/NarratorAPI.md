# Narrator API Documentation

The lua wrapper for C# Anthropic.SDK and memory management tools.

## Core Methods

### Message **AsAssistantMessage(string Message)**
Creates a new `Message` as assistant using the parameter message.
#### Parameters
- **Message** (string): The message to be marked as `Role.Assistant`

### Message **AsSystemMessage(string Message)** 
Creates a new `Message` as system using the parameter message.
#### Parameters
- **Message** (string): The message to be marked as `Role.System`

### async Table **think(dynamic Messages)**
Prompts Anthropic with requests and returns a response.
#### Parameters
- **Messages** (dynamic): Can be either `string` or `DynValue.Table` depending on number of messages
#### Returns
- **ResponseTable** (Table): Contains response information
  - **content** (string): The response from Anthropic
  - **messages** (Table): Contains all messages including request and response
#### Usage
```lua
-- String method
local response = narrator:think(narrator:prompt(narrator:mods:weatherdotcom("get_local_weather","Dubai, UAE")))
narrator.say(response.content)

-- Table method
local response = narrator:think({
    "What's the weather like today?",
    AsAssistantMessage("Sure! Could you please provide me with your location?"),
    "Dubai, UAE"
})

print("Response from Anthropic: " .. response.content)
```

## Global Memory System
System for managing state between mods.

### uservars
Use uservars for variable storage. Variables are created when prompting.
#### Usage
```lua
uservars.test = "new var created"
uservars.test = "another new var" 
-- Pause script, prompt script if it wants to overwrite data. Data: "new var created". Run the script again when ready.
```

## Narrator Component
The narrator is the main component used to call different functions per mod. Named in the mod's header.
Best practice is to explicitly call narrator mod methods using `:`.

### chirp
Audio output methods.
```lua
chirp:say(voice, text) -- Output text-to-speech
chirp:voice_search(query) -- Search for voice profile
```

### jukebox  
Music/audio playback methods.
```lua
jukebox:play_music(track) -- Play music track
jukebox:music_search(query) -- Search for music
```

## Events/Hooks
- **Awake**: Called when mod is first loaded
- **Start**: Called before first frame 
- **Update**: Called every frame (16ms)

Use these sparingly and only when needed for continuous operations.

## Best Practices
1. Use uservars for all state management
2. Make direct API calls without wrappers
3. Avoid creating functions
4. Trust the API for error handling
5. Keep code minimal and focused
6. Use narrator: prefix for clarity
7. Leverage existing components instead of creating new ones