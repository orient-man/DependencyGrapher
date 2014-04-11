using System.IO;
using NDesk.Options;

namespace DependencyGrapher
{
    class Program
    {
        private static void Main(string[] args)
        {
            string inputFile = "";
            string outputFile = "";
            var finderOptions = new DependencyFinderOptions();
            var cmdLineOptions = new OptionSet
            {
                { "i=|input=", v => inputFile = v },
                { "o=|output=", v => outputFile = v },
                { "hideTransitive", v => finderOptions.RemoveTransitiveReferences = v != null },
                { "asmInclude=", v => finderOptions.AssemblyIncludeRegex = v },
                { "asmExclude=", v => finderOptions.AssemblyExcludeRegex = v },
                { "interfaceInclude=", v => finderOptions.InterfaceIncludeRegex = v },
            };

            cmdLineOptions.Parse(args);

            using (var finder = new DependencyFinder(inputFile, finderOptions))
                File.WriteAllText(outputFile, new DotModuleDiagram().Draw(finder.FindDependencies()));
        }
    }
}