using System.IO;

namespace DependencyGrapher
{
    class Program
    {
        private static void Main(string[] args)
        {
            using (var diagram = new DotModuleDiagram(args[0]))
                File.WriteAllText(args[1], diagram.Draw());
        }
    }
}