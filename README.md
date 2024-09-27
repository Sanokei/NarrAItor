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
local response = narrator:think(narrator:prompt({cal_response, {"NarratorPersonality","Adventurous Persona"}}))
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