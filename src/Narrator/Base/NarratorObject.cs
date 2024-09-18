using System.IO;
using Newtonsoft.Json;
using NarrAItor.Narrator.NarratorExceptions;
namespace NarrAItor.Narrator
{
    public abstract class NarratorObject : INarratable
    {
        // System Defaults
        private const string DEFAULT_DOCUMENTATION_PATH = "./Narrator/ChripingNarrator/docs/";
        private const string DEFAULT_DOCUMENTATION = "";

        private string _Name = "DefaultNarrator";
        public string Name
        {
            get { return _Name; }
            set { _Name = value; }
        }
        private string _Version = "0.0.0";
        public string Version
        {
            get { return _Version; }
            set { _Version = value; }
        }
        private string _Objective = "You are a Narrator";
        public string Objective
        {
            get { return _Objective; }
            set { _Objective = value; }
        }
        private string _Personality = "You act like the Narrator from every movie intro";
        public string Personality
        {
            get { return _Personality; }
            set { _Personality = value; }
        }
        private string _DocumentationPath = DEFAULT_DOCUMENTATION_PATH;
        public string DocumentationPath
        {
            get { return _DocumentationPath; }
            set { _DocumentationPath = value; }

        }

        // Gets the Documentation from the path
        public string Documentation = "";
        public string GetDocumentation(string path = DEFAULT_DOCUMENTATION_PATH, bool useCachedDocumentation = true)
        {
            string DirPath = Path.Combine(Directory.GetCurrentDirectory(), path.Equals(DEFAULT_DOCUMENTATION_PATH) ? path : DocumentationPath);
            if(!Directory.Exists(DirPath))
            {
                Console.WriteLine(Warnings.DOCUMENTATION_PATH_DOES_NOT_EXIST);
                return DEFAULT_DOCUMENTATION;
            }    

            if(Documentation != "" && useCachedDocumentation)
                return Documentation;

            Dictionary<string, string[]> DocFiles = [];
            foreach (string fileName in Directory.GetFiles(DirPath, "*.md"))
                DocFiles.Add(fileName,File.ReadAllLines(fileName));
            Documentation = JsonConvert.SerializeObject(DocFiles);
            return Documentation;
        }
    }
}