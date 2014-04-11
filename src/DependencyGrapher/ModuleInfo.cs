namespace DependencyGrapher
{
    public class ModuleInfo
    {
        public string Name { get; private set; }
        public ModuleInfo[] Dependencies { get; set; }
        public string[] DomainObjects { get; set; }

        public ModuleInfo(string name)
        {
            Name = name;
        }
    }
}