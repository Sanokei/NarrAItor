# Lua binding API
The lua API that wraps the C# Anthropic.SDK for the LLM to use to create functionality.

## Anthropic.SDK
The C# package being wrapped
### Message AsAssistantMessage(string Message)
Creates a new `Message` as assistant using the parameter message.
#### parameters
##### string __Message__
The message to be marked as `Role.Assistant`

## narrator
The narrator is the main component that can be used to call differing functions. Can be used in lua just as an object `narrator`. It's best practice to explictly call narrator methods using `:`.

## Narrator Methods
The methods to interact with the NarratorAPI
### string __prompt(Table kwargs)__
Standardized prompt to be able to get back responses that fit the API schema.
> [!NOTE]
> The kwargs will be embedded into the global of the script, as variables to use under `uservars`
#### parameters
##### Table kwargs
kwargs should be formatted to give every arg a keyword.
#### usage
```lua
narrator:prompt({
    {"username","William"},
    {"voice","adventurous"}
}) 
```

### void __print(string Phrase)__
A debug function that prints a string to the console. Same function as `print("")`.
#### parameters
##### string __Phrase__
The printable phrase
#### usage
``` lua
narrator:print("Hello World")
```

### async void __say(string Voice, string Phrase)__
Uses <> to say a phrase using a specific voice.
#### parameters
##### string __Voice__
The voice of the ai
##### string __Phrase__
The phrase to be spoken
#### usage
``` lua
narrator:say("Robot","Hello World")
```

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
