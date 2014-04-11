using System;
using System.IO;
using NDesk.Options;

namespace DependencyGrapher
{
    class Program
    {
        private static void Main(string[] args)
        {
            var showHelp = false;
            var inputFile = "";
            var outputFile = "";
            var finderOptions = new DependencyFinderOptions();

            var cmdLineOptions = new OptionSet
            {
                { "h|help", "show this message and exit", v => showHelp = v != null },
                { "i=|input=", "input root assembly", v => inputFile = v },
                { "o=|output=", "output dot diagram file", v => outputFile = v },
                {
                    "hideTransitive",
                    "hide transitive dependencies (default: false)",
                    v => finderOptions.RemoveTransitiveReferences = v != null
                },
                {
                    "asmInclude=",
                    "Regex for including referenced assemblies (default: all)",
                    v => finderOptions.AssemblyIncludeRegex = v
                },
                {
                    "asmExclude=",
                    "Regex for excluding referenced assemblies (default: none)",
                    v => finderOptions.AssemblyExcludeRegex = v
                },
                {
                    "typeInclude=",
                    "Regex for including types (default: none)",
                    v => finderOptions.TypeIncludeRegex = v
                },
                {
                    "typeExclude=",
                    "Regex for excluding types (default: all)",
                    v => finderOptions.TypeExcludeRegex = v
                },
            };

            try
            {
                cmdLineOptions.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("DependencyGrapher: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `DependencyGrapher --help' for more information.");
                return;
            }

            if (showHelp || string.IsNullOrEmpty(inputFile))
            {
                ShowHelp(cmdLineOptions);
                return;
            }

            var diagram = DrawDependeciesGraph(inputFile, finderOptions);
            if (string.IsNullOrEmpty(outputFile))
                Console.Write(diagram);
            else
                File.WriteAllText(outputFile, diagram);
        }

        private static void ShowHelp(OptionSet cmdLineOptions)
        {
            Console.WriteLine("Usage: DependencyGrapher [OPTIONS]+ message");
            Console.WriteLine();
            Console.WriteLine("Options:");
            cmdLineOptions.WriteOptionDescriptions(Console.Out);
        }

        private static string DrawDependeciesGraph(
            string inputFile,
            DependencyFinderOptions finderOptions)
        {
            using (var finder = new DependencyFinder(inputFile, finderOptions))
                return new DotModuleDiagram().Draw(finder.FindDependencies());
        }
    }
}