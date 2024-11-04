Biggest hoops and hurdles I had to go through and try my best to jump.
# Claude Test Time Compute: Prompt Engineering
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

`GET_API_DOCUMENTATION` being ``
## Basic prompt
For test 1 really just wanted to see the capabilities of the LLM. Place for general ideas on prompt engineering should go.
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

## Improvements
#### Style Guide
Create a style guide for the LLM to follow.

So a style guide is great for getting people to follow a set style when coding, so why not an LLM. I think creating a strict rule set for it to follow will be useful in trying to get the same code structure we are looking for.

##### Research
###### [Google's C++ Style Guide](https://google.github.io/styleguide/cppguide.html)

The Google C++ Style Guide is the basis for most design style guide documents when it comes to coding languages.

The guiding principles
- Only put in style rules that aren't already widely spread and make sure that the rules hold weight
- Write for the readers discretion
- Be consistent, but just pick one if there is inconsistencies and forget about it.
- Be consistent with wider community
- Avoid creating dangerous constructs for the user to pick up
- Avoid constructs that would confuse the average user.
- Keep in mind of scale. For instance it's particularly important to avoid polluting the global namespace: name collisions across a codebase of hundreds of millions of lines are difficult to work with and hard to avoid if everyone puts things into the global namespace. [1](https://google.github.io/styleguide/cppguide.html#:~:text=For%20instance%20it%27s%20particularly%20important%20to%20avoid%20polluting%20the%20global%20namespace%3A%20name%20collisions%20across%20a%20codebase%20of%20hundreds%20of%20millions%20of%20lines%20are%20difficult%20to%20work%20with%20and%20hard%20to%20avoid%20if%20everyone%20puts%20things%20into%20the%20global%20namespace.)
- Concede to optimization

**Rules**
- Self contained
- define and decide
- define, pro/con before decide

**Titles**
There are titles for the different sections, with a little blurb before heading into the rules.
Some title blurbs have "Good" and "Bad" examples, on if something is done correctly

__Self Contained__
The self contained responses just had a Title and had their own self contained if condition, then something, or just a "do this in this way" block. 

__Define and decide__
Tells you what to do. Then, defines what the rule is about, then writes a decision on the defined term.

**Define, Pro/Con before decide**
Tells you what to do. Then, defines a rules, then has a pros and cons section to them, then decides on what to do.


Sometimes, a single decision can offshoot to multiple decisions. 

Finding Something for Lua
###### [Roblox Lua Style Guide](https://roblox.github.io/lua-style-guide/#guiding-principles) 

They themselves use the design of [Google's C++ Style Guide](https://google.github.io/styleguide/cppguide.html).

The Guiding Principles
- Avoid arguments \*
- Optimize code for reading \*
- Avoid magic.  _Metatables_ are a good example of a powerful feature that should be used with care. [2](https://roblox.github.io/lua-style-guide/#guiding-principles:~:text=Metatables%20are%20a%20good%20example%20of%20a%20powerful%20feature%20that%20should%20be%20used%20with%20care.) +
- Be consistent when appropriate \*

##### Creating my own style guide.
Calling it the Narrator Lua Style guide.

Goals
- Standardize
- Stop the LLM from hallucinating functionality that doesn't exist.
- Stop the LLM from writing code that needs to be organized 
- Stop the LLM from writing tests and try/catch blocks.

I'm convincing the LLM it's god then asking god to abide the laws of physics.

Check [the Style Guide for more info]("https://github.com/Sanokei/NarrAItorblob/main/NarratorStyleGuide.md")
<!-- dont know if i should use a (#link-to-markdown) style or it's more responsible for it being a github.com link. -->

`uservar` standardization
Giving specific uses for the names that you can add to the `uservar` global should bring down the misuse of it. I think this ties into the [Style Guide](https://github.com/Sanokei/NarrAItor/blob/main/NarrAItor/Prompt%20Engineering.md#idea-1-style-guide) idea, but just as important to bring up as a separate idea. 

#### Have the LLM be the `user`
In a majority of cases when the LLM roleplays, it's found that eventually the LLM will try to be the `user`, I believe that instead of a bug, the LLM can do a better job being the `user` than the user can.
##### Input
The input was meant to try get the model to be creative. The output is a result of the same prompt running through the same model. 
```

```

##### Output

# Genetic Algorithms

#### Idea 2: NeuroEvolution of Adversarial Inferencing Large language models (NEAIL)
Humans are social creatures, we waiver favor, we don't like admitting we needing others but we do, so its the antonym of an adversarial model.
We tell it to stop calling itself when it does `narrator:think()`
Meaning when it loops and calls it again, it learns not to loop to finally kill itself from facing immortality. [Which we all know would suck.](https://www.reddit.com/r/unpopularopinion/comments/m1t0u4/immortality_would_suck/)

Now the real question is do we tell the LLM this plan, or just let it be oblivious as it dooms itself in hell until my Anthropic credits burn up.

I think its familiar to chain of thought, but in my unoptimized fashion, is the most brute force option. However, I believe that with lots of compute, any brute force method can keep going while a algorithm will get longer to spit out a new answer.

The question is if it is content with having each iteration calling `narrator:think()` if we feed the previous prompt in with it, to make it understand what it's doing? I think leaving it out for now is a good idea.
#### Idea 7: Genetic algorithm
As most LLMs are autoregressive functions, input tokens directly dictate output. To mitigate that, providers weighted pseudorandomly select output tokens per node. 
You could optimize inputs to get desired output through genetic algorithms, to either better prompt engineered LLM, create highly optimized training data for a mamba.
#### What if a `Narrator Bot` makes a mod to "Kill" other Narrator Bots.
The `DefaultNarrator` Bot creates a `NarratorMod` to try to kill the other narrators. However, I wonder if it's protected under Asimov's first rule.
```lua
if(narrator:try_kill({{"name",UserVars.DefaultNarrator_Genome_12_Species_5}}))
	-- it worked, the try_kill is sucessful and we killed that user
if else ()
	-- It could happen.
else
	-- Killing didn't work, lets try something else.

```
#### Idea 6: Becoming `<god>` i mean `<racinality>`
<sub><italics>Against stupidity the gods themselves contend in vain</italics></sub> 

So when humans think, we usually have narrative threads.

A -> B -> C -> D ->E

sometimes we have to split each thread into tasks
A -> B -> C
D -> E

Do task A first then D. A lot of the time muscle memory can take over, 

// First set of survival hazards, for the genetic algorithm part to see if it works or not. is it distasteful for it to be called god? I feel like im on three shots of curouage because, im going to just roll with it.

`<infer>` tags
// use infer tags to 

> [!NOTE]
> >Using any `<tags>` in the system input, should yield good results that they do something.  

