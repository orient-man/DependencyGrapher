using System.IO;

namespace DependencyGrapher
{
    class Program
    {
        private static void Main(string[] args)
        {
            File.WriteAllText(args[1], new DotModuleDiagram().Draw(args[0]));
        }
    }
}