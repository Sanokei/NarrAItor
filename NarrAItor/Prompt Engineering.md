So my biggest hurdle at the moment would be that Anthropic isn't giving back any solid answers for the prompt I have in place.

The current prompt is this:
```
Using the following data:
{String.Join(",",args.Select(p=>p.ToString()))},
{PROMPT}
The response must be within {MaxTokens.ToString()} number of Tokens.\
Do NOT make up API endpoints. Only use the avaiable API below\
{GET_API_DOCUMENTATION}
```

`PROMPT` being `Create a lua program to create a narration in the style specified. That uses the voice specified. Only return the lua code. Do not use ``` to make it a code block. As if you returned anything else, it will break.`

## Test 1: Basic prompt
For test 1 really just wanted to see the capabilities of the LLM
### Input/output
The input was meant to try get the model to be creative. The output is a result of the same prompt running through the same model. 
#### Input
The input was purposely created to promote creativity within the LLM.
```lua
local response = narrator:think(narrator:prompt("in the stlye of the skylord of the sea, drawf of stone.","voice like Gordon from HL1"))

print("Response from Anthropic: " .. response.content)
```
#### Output
Interestingly, multiple times, it gave this exact same result.
```lua
local function createNarration()
    local style = "in the style of the skylord of the sea, drawf of stone."
    local voice = "voice like Gordon from HL1"
    
    local prompt = narrator:prompt(style, voice)
    local response = narrator:think(prompt)
    
    if response and response.content then
        narrator:say(voice, response.content)
    else
        narrator:print("Failed to generate narration.")
    end
end

createNarration()
```

### Thoughts from input/output
- After running the program multiple times it was convinced that it had to create its own functions and error handling. 
- Instead of being creative, it just passes off the work to itself through prompt and think methods.

### Ideas of improvement

#### Idea 1: Style Guide
Create a style guide for the LLM to follow.

So a style guide is great for getting people to follow a set style when coding, so why not an LLM. I think creating a strict rule set for it to follow will be useful in trying to get the same code structure we are looking for.


