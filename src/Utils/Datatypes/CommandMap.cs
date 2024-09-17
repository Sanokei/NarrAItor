using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NarrAItor.Utils.Datatypes
{
    public class CommandMap : IEnumerable
    {
        public delegate void CommandAction(string[] args);

        private Dictionary<string, CommandAction> _Commands = [];

        public void AddCommand(CommandAction action, params string[] aliases)
        {
            foreach (var alias in aliases)
            {
                _Commands[alias] = action;
            }
        }

        public bool TryGetCommand(string input, out CommandAction? action)
        {
            return _Commands.TryGetValue(input, out action);
        }

        public IEnumerable<string> GetAliases(CommandAction action)
        {
            return _Commands.Where(kvp => kvp.Value == action).Select(kvp => kvp.Key);
        }

        public bool RemoveAlias(string alias)
        {
            return _Commands.Remove(alias);
        }
        /// <summary>
        /// Allows the deletion of commands through outside sources.
        /// </summary>
        /// <param name="action"></param>
        public void RemoveCommand(CommandAction action)
        {
            _Commands.Where(kvp => kvp.Value == action).Select(kvp => _Commands.Remove(kvp.Key));
        }


        // public void Clear()
        // {
        //     _Commands.Clear();
        // }
        
        public int Count => _Commands.Count;
        [Obsolete("This returns every alias individually. Meant for internal use only.")]
        public IEnumerator<KeyValuePair<string, CommandAction>> GetEnumerator()
        {
            return _Commands.GetEnumerator();
        }
        [Obsolete("This returns every alias individually. Meant for internal use only.")]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}