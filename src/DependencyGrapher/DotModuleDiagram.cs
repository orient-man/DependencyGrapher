using System.Collections.Generic;
using System.Linq;

namespace DependencyGrapher
{
    public class DotModuleDiagram
    {
        public string Draw(IEnumerable<ModuleInfo> moduleMap)
        {
            var modules = "";
            var relations = "";
            foreach (var m in moduleMap.OrderBy(o => o.Name))
            {
                var moduleName = FormatModuleName(m.Name);
                modules += FormatModule(moduleName, m.DomainObjects);
                relations = m.Dependencies
                    .Aggregate(
                        relations,
                        (current, dep) => current + FormatRelation(moduleName, dep));
            }

            return "digraph Pincasso {\n" +
                   "\trankdir=LR\n" +
                   "\tnode [shape=plaintext]\n" +
                   "\tedge [arrowsize=0.75 fontsize=14 fontcolor=blue]\n\n" +
                   modules + "\n" + relations + "}";
        }

        private static string FormatModule(
            string name,
            IEnumerable<string> domainObjects)
        {
            return "\t" + name + " [label=<\n" +
                   "<table border='0' cellborder='1' cellspacing='0' cellpadding='2'>\n" +
                   "\t<tr><td bgcolor='lightblue'>" +
                   name + "</td></tr>\n" +
                   "\t<tr><td><table border='0' cellborder='0' cellspacing='0' cellpadding='0'>\n" +
                   "\t\t<tr><td align='left'>" +
                   domainObjects
                       .Aggregate(
                           (current, domainObject) =>
                               current +
                               "</td></tr>\n\t\t<tr><td align='left'>" +
                               domainObject) +
                   "</td></tr>\n\t</table></td></tr>\n" +
                   "</table>>];\n";
        }

        private static string FormatRelation(string name, ModuleInfo dep)
        {
            return "\t" + name + " -> " + FormatModuleName(dep.Name) + ";\n";
        }

        private static string FormatModuleName(string assemblyName)
        {
            var postfix = assemblyName.Replace("Pincasso.", "");
            return postfix == "Core" ? "Core" : postfix.Replace(".Core", "");
        }
    }
}