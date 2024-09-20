using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NarrAItor.Utils.Datatypes;

public class CommandMap : IEnumerable
{
    public delegate Task AsyncCommandAction(string[] args);

    private Dictionary<string, Delegate> _Commands = new();

    public void AddCommand(Delegate action, params string[] aliases)
    {
        if (action is not Action<string[]> && action is not AsyncCommandAction)
        {
            throw new ArgumentException("Action must be either Action<string[]> or Func<string[], Task>", nameof(action));
        }

        foreach (var alias in aliases)
        {
            _Commands[alias] = action;
        }
    }

    public bool TryGetCommand(string input, out Delegate? action)
    {
        return _Commands.TryGetValue(input, out action);
    }

    public IEnumerable<string> GetAliases(Delegate action)
    {
        return _Commands.Where(kvp => kvp.Value == action).Select(kvp => kvp.Key);
    }

    public bool RemoveAlias(string alias)
    {
        return _Commands.Remove(alias);
    }

    public void RemoveCommand(Delegate action)
    {
        var aliasesToRemove = _Commands.Where(kvp => kvp.Value == action).Select(kvp => kvp.Key).ToList();
        foreach (var alias in aliasesToRemove)
        {
            _Commands.Remove(alias);
        }
    }

    public int Count => _Commands.Count;

    [Obsolete("This returns every alias individually. Meant for internal use only.")]
    public IEnumerator<KeyValuePair<string, Delegate>> GetEnumerator()
    {
        return _Commands.GetEnumerator();
    }

    [Obsolete("This returns every alias individually. Meant for internal use only.")]
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}