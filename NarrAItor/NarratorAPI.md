The lua wrapper for C# Anthropic.SDK, and some other memory management tools. 
## Anthropic.SDK Methods
The C# package being wrapped
### Message **AsAssistantMessage(string Message)**
Creates a new `Message` as assistant using the parameter message.
#### parameters
##### string __Message__
The message to be marked as `Role.Assistant`
### Message **AsSystemMessage(string Message)**
Creates a new `Message` as system using the parameter message.
#### parameters
##### string __Message__
The message to be marked as `Role.System`

> [!NOTE] `narrator`
> The narrator is the main component that can be used to call differing functions per mod. Named in the mod's header. It's best practice to explicitly call narrator mod methods using `:`.
### async Table __think(dynamic Messages)__ 
Prompts Anthropic with requests and returns a response.
#### parameters
##### dynamic Messages
Can either be of type `string` or `DynValue.Table` depending on the number of messages in the request.
#### return value
##### Table __ResponseTable__
The table that contains the LLM reponse information.
###### string __ResponseTable.content__
The response from Anthropic as a string value.
###### Table __ResponseTable.messages__
Contains all messages including the response, Messages and Response. 
#### usage
``` lua
-- String method
local response = narrator:think(narrator:prompt(narrator:mods:weatherdotcom("get_local_weather","Dubai, UAE")))
narrator.say(response.content)

-- Alternatively you can use the Table method
local response = narrator:think({
                "What's the weather like today?",
                AsAssistantMessage("Sure! Could you please provide me with your location?"),
                "Dubai, UAE"
            })
        
print("Response from Anthropic: " .. response.content)
```

## Untitled Global Memory System
Adds a global memory standard between mods.
### uservars
Use the uservars for variable use. There are uservars that are created when prompting.
`uservars.<variable name>` moniker instead.
#### usage
```lua
uservars.test = "new var created"
uservars.test = "another new var" 
-- Pause script, prompt script if it wants to overright data. Data: "new var created". Run the script again when ready.
```