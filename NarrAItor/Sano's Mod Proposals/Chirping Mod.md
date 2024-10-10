```
@Modname chirp
@Version 0.0.1
```
## Methods
### async void __say(string phrase)__
Calls say(Table options, string phrase) with an empty table.
#### usage
``` lua
chirp:say("Hello World")
```
### async void __say(Table options, string phrase)__
Uses <> API to say a phrase using a specific voice.
#### parameters
##### Table __options__
Used to set different options:
- `voice` of the AI.
- `location`
##### string __phrase__
The phrase to be spoken
#### usage
``` lua
chirp:say({{"voice",chirp:search_voice("Robot")},{"location","concert hall"}}, "Hello World")
```
