namespace NarrAItor.Utils.Datatypes;

public class Section<TDerived> where TDerived : Section<TDerived>
    {
        public TDerived Parent { get; private set; }
        public List<TDerived> Children { get; private set; }
        public Section(TDerived parent)
        {
            this.Parent = parent;
            this.Children = new List<TDerived>();
            if(parent!=null) { parent.Children.Add((TDerived)this); }
        }
        public bool isRoot { get { return Parent == null; } }
        public bool hasChildren { get { return Children.Count!=0; } }
        public List<TDerived> GetLeafNodes()
        {
            List<TDerived> AllChildren = [];
            foreach(var child in Children)
                if(child.hasChildren)
                    AllChildren.AddRange(child.GetLeafNodes());
                else
                    AllChildren.Add(child);
            return AllChildren;
        }
    }

    public class ConfigurationSections : Section<ConfigurationSections>
    {
        protected ConfigurationSections() : base(null) {}
        protected ConfigurationSections(ConfigurationSections parent) : base(parent) {}
        public string ConfigurationName {get;set;}
        public string Configuration
        {
            get
            {
                return isRoot ? ConfigurationName : Parent.Configuration == null ? ConfigurationName : $"{Parent.Configuration}__{ConfigurationName}";
            }
        }
        public static ConfigurationSections Create()
        {
            return new ConfigurationSections{ };
        }
        public ConfigurationSections Create(string Config)
        {
            return new ConfigurationSections(this){ ConfigurationName = Config};
        }
    }