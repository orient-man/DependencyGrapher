namespace DependencyGrapher
{
    public class DependencyDiagramOptions
    {
        public bool HideTransitiveReferences { get; set; }
        public string AssemblyIncludeRegex { get; set; }
        public string AssemblyExcludeRegex { get; set; }
        public string InterfaceIncludeRegex { get; set; }

        public DependencyDiagramOptions()
        {
            AssemblyIncludeRegex = @"^Pincasso\..*Core$";
            AssemblyExcludeRegex = @"^Pincasso\.Migracja\.Core$";
            InterfaceIncludeRegex = @"^IPincassoDomainObject.*";
        }
    }
}