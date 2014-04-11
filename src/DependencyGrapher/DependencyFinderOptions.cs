namespace DependencyGrapher
{
    public class DependencyFinderOptions
    {
        public bool RemoveTransitiveReferences { get; set; }
        public string AssemblyIncludeRegex { get; set; }
        public string AssemblyExcludeRegex { get; set; }
        public string TypeIncludeRegex { get; set; }
        public string TypeExcludeRegex { get; set; }
    }
}