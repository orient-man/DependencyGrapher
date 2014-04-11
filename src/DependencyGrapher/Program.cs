using System.IO;

namespace DependencyGrapher
{
    class Program
    {
        private static void Main(string[] args)
        {
            using (var finder = new DependencyFinder(args[0]))
                File.WriteAllText(args[1], new DotModuleDiagram().Draw(finder.FindDependencies()));
        }
    }
}