namespace DependencyGrapher
{
    public class DependencyFinderOptions
    {
        public bool RemoveTransitiveReferences { get; set; }
        public string AssemblyIncludeRegex { get; set; }
        public string AssemblyExcludeRegex { get; set; }
        public string InterfaceIncludeRegex { get; set; }

        public DependencyFinderOptions()
        {
            AssemblyIncludeRegex = @"^Pincasso\..*Core$";
            AssemblyExcludeRegex = @"^Pincasso\.Migracja\.Core$";
            InterfaceIncludeRegex = @"^IPincassoDomainObject.*";
        }
    }
}