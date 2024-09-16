using System.IO;
using Newtonsoft.Json;
using NarrAItor.Narrator.NarratorErrors;
namespace NarrAItor.Narrator
{
    public abstract class NarratorObject : INarratable
    {
        // System Defaults
        private const string DEFAULT_DOCUMENTATION_PATH = "./docs/";
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
        private string _DocumentationPath = DEFAULT_DOCUMENTATION_PATH;
        public string DocumentationPath
        {
            get { return _DocumentationPath; }
            set { _DocumentationPath = value; }

        }

        // Gets the Documentation from the path
        public string? Documentation;
        public string GetDocumentation(string path = DEFAULT_DOCUMENTATION_PATH, bool useCachedDocumentation = true)
        {
            if(!Directory.Exists(path))
            {
                Console.WriteLine(Warnings.DOCUMENTATION_PATH_DOES_NOT_EXIST);
                return DEFAULT_DOCUMENTATION;
            }    

            if(Documentation != null && useCachedDocumentation)
                return Documentation;

            string DirPath = Path.Combine(Directory.GetCurrentDirectory(), path.Equals(DEFAULT_DOCUMENTATION_PATH) ? path : DocumentationPath);
            Dictionary<string, string[]> DocFiles = [];
            foreach (string fileName in Directory.GetFiles(DirPath, "*.md"))
            {
                DocFiles.Add(fileName,File.ReadAllLines(fileName));
            }
            Documentation = JsonConvert.SerializeObject(DocFiles);
            return Documentation;
        }

        // Uses Documentation to get the "Abridged" version.
        public string AbridgedDocumentation = "";
        public string GetAbridgedDocumentation()
        {
            if(Documentation == null)
                GetDocumentation();
            
            throw new NotImplementedException();
        }
    }
}