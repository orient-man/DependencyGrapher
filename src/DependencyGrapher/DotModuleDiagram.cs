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
                modules += FormatModule(m.Name, m.DomainObjects);
                relations = m.Dependencies
                    .Aggregate(
                        relations,
                        (current, dep) => current + FormatRelation(m.Name, dep.Name));
            }

            return "digraph Pincasso {\n" +
                   "\trankdir=LR\n" +
                   "\tnode [shape=plaintext]\n" +
                   "\tedge [arrowsize=0.75 fontsize=14 fontcolor=blue]\n\n" +
                   modules + "\n" + relations + "}";
        }

        private static string FormatModule(string name, string[] domainObjects)
        {
            return "\t" + name.Replace(".", "") + " [label=<\n" +
                   "<table border='0' cellborder='1' cellspacing='0' cellpadding='2'>\n" +
                   "\t<tr><td bgcolor='lightblue'>" + name + "</td></tr>\n" +
                   FormatDomainObject(domainObjects) +
                   "</table>>];\n";
        }

        private static string FormatDomainObject(string[] domainObjects)
        {
            if (domainObjects.Length == 0)
                return "";

            return "\t<tr><td><table border='0' cellborder='0' cellspacing='0' cellpadding='0'>\n" +
                   "\t\t<tr><td align='left'>" +
                   domainObjects
                       .Aggregate(
                           (current, domainObject) =>
                               current +
                               "</td></tr>\n\t\t<tr><td align='left'>" +
                               domainObject) +
                   "</td></tr>\n\t</table></td></tr>\n";
        }

        private static string FormatRelation(string name, string depName)
        {
            return "\t" + name.Replace(".", "") + " -> " + depName.Replace(".", "") + ";\n";
        }
    }
}