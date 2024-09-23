# NarrAItor
A narrator that comments on your life. Moddable using a lua Anthropic binder.

## What is NarrAItor?
A lua binding with Anthropic made for an LLM to use to create a narration of a person's life.
When running the program, Anthropic will be prompted to create a lua script using the API below to create a customized narrator for your life.

### basic example
``` lua
-- Sure, here is an default narration.
narrator:play_music(narrator:music_search("adventurous")[1].title)
narrator:say("Morrow_Wind_Character","And as the adverntuer woke up from his mighty nap he thought about the day before him.")
```
With more personal information from the user (e.g access to scheduling software) the narration can become more nuanced and complex.

### using personal info example
``` lua
-- Sure, here is a narration using the google calender information provided.
narrator:play_music(narrator:music_search("adventurous")[1].title)
-- as an example let's say we already have output cal_response.
local cal_response = "And as the adverntuer woke up awaiting the meeting with fellow dwarves at sundown of 7, they venture forth to the work day ahead of them starting at 9:30 in thy morning."
narrator:say("Morrow_Wind_Character",cal_response)
```

Another trick up the LLM's metaphorical sleeves is the ability to pass off work to be able to use tools at its disposal.
``` lua
-- Sure, here is a complex narration with tool use.
local cal_response = narrator:get_info("calander__google")
local response = narrator:think(narrator:prompt(cal_response, "Adventurous Persona"))
narrator:say(response.content)
```


## Basic Instructions
Have .Net 8 installed

Windows x64
``` cmd
rem Clone project
$ git clone https://github.com/Sanokei/NarrAItor.git folder-name
$ cd folder-path

rem Create secret
$ dotnet user-secrets init

rem Set Anthropic API key
$ dotnet user-secrets set "Anthropic__BearerToken" "x-api-key"

rem Run the script
$ dotnet run
```
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
### string __prompt(params string[] args)__
Standardized prompt to be able to get back responses that fit the API schema.
> [!NOTE]
> Because params isn't an option in lua, it is abstracted with range of [0,5] arguments.
#### parameters
##### params string[] args
0 to 5 string arguments that will be added to the prompt.

### string __prompt_stringonly__
Standaridized prompt that makes Anthropic only send back the desired DynValue string response without any fluff. 

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
