namespace NarrAItor.Narrator
{
    public interface INarratable
    {
        public string Name{get;set;}
        public string Version{get;set;}
        /// <summary>
        /// The overall objective of the narrator, not current state.
        /// </summary>
        public string Objective{get;set;}
        /// <summary>
        ///  The relative path of the narrator's documentation.
        /// </summary>
        public string DocumentationPath{get;set;}

    }
}