A prompt engineer. Uses multiple prompt engineering ideas [[PromptEngineering]].
```
@Modname liam
@Version 0.0.1
```
Prompt engineering ideas:
- prompt creator
- style guide
// TODO: be able to enable and disabled
## Methods
The only method Liam needs is will power.
### string __prompt(Table kwargs)__
Standardized prompt to be able to get back responses that fit the API schema.
> [!NOTE]
> The kwargs will be embedded into the memory of the script, as variables to use under `uservars`
#### parameters
##### Table kwargs
kwargs should be formatted to give every arg a keyword.
#### usage
```lua
liam:prompt({
    {"username","Liam"},
    {"voice","adventurous"}
}) 
```