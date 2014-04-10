﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace DependencyGrapher
{
    public class DotModuleDiagram
    {
        private readonly Regex moduleIncludeRegex = new Regex(@"^Pincasso\..*Core$");
        private readonly Regex moduleExcludeRegex = new Regex(@"^Pincasso\.Migracja\.Core$");

        private readonly Dictionary<Assembly, Assembly[]> moduleMap =
            new Dictionary<Assembly, Assembly[]>();

        private string assemblyPath;

        public string Draw(string rootAssemblyPath, bool hideTransitiveReferences = false)
        {
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve +=
                CurrentDomain_ReflectionOnlyAssemblyResolve;

            var rootAssembly = Assembly.ReflectionOnlyLoadFrom(rootAssemblyPath);
            assemblyPath = GetAssemblyPath(rootAssembly);
            FindDependencies(rootAssembly);

            if (hideTransitiveReferences)
                RemoveTransitiveReferences();

            return DrawModuleMap();
        }

        Assembly CurrentDomain_ReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var fileName = assemblyPath + GetAssemblyName(args.Name);
            if (File.Exists(fileName))
                return Assembly.ReflectionOnlyLoadFrom(assemblyPath + GetAssemblyName(args.Name));

            return Assembly.ReflectionOnlyLoad(args.Name);
        }

        private void FindDependencies(Assembly module)
        {
            var name = module.GetName().Name;
            if (moduleMap.Keys.Any(o => o.GetName().Name == name))
                return;

            var dependencies = module
                .GetReferencedAssemblies()
                .Where(o => IsModule(o.Name))
                .OrderBy(o => o.Name)
                .Select(o => Assembly.ReflectionOnlyLoadFrom(assemblyPath + GetAssemblyName(o.FullName)))
                .ToArray();

            if (IsModule(name))
                moduleMap[module] = dependencies;

            foreach (var dep in dependencies)
            {
                FindDependencies(dep);
            }
        }

        private string GetAssemblyName(string fullName)
        {
            var idx = fullName.IndexOf(',');
            return (idx < 0 ? fullName : fullName.Substring(0, idx)) + ".dll";
        }

        private string GetAssemblyPath(Assembly assembly)
        {
            var path = assembly.Location;
            var idx = path.LastIndexOf(Path.DirectorySeparatorChar);
            if (idx < 0)
                return "";

            return path.Substring(0, idx + 1);
        }

        private bool IsModule(string moduleName)
        {
            return moduleIncludeRegex.IsMatch(moduleName) &&
                   !moduleExcludeRegex.IsMatch(moduleName);
        }

        private void RemoveTransitiveReferences()
        {
            foreach (var module in moduleMap.Keys.ToArray())
            {
                var dependencies = moduleMap[module];
                moduleMap[module] =
                    dependencies
                        .Where(
                            o =>
                                dependencies
                                    .SelectMany(GetAllDependencies).ToList()
                                    .All(d => d != o))
                        .ToArray();
            }
        }

        private IEnumerable<Assembly> GetAllDependencies(Assembly module)
        {
            return moduleMap[module]
                .Concat(moduleMap[module].SelectMany(GetAllDependencies));
        }

        private string DrawModuleMap()
        {
            var modules = "";
            var relations = "";
            foreach (var m in moduleMap.Keys.OrderBy(o => o.GetName().Name))
            {
                var moduleName = FormatModuleName(m);
                modules += FormatModule(moduleName, GetDomainObjects(m));
                relations = moduleMap[m]
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

        private static IEnumerable<string> GetDomainObjects(Assembly module)
        {
            return module.GetTypes()
                .Where(
                    o => o
                        .GetInterfaces()
                        .Any(i => i.Name.Contains("IPincassoDomainObject")))
                .OrderBy(o => o.Name)
                .Select(o => o.Name.Substring(1))
                .DefaultIfEmpty();
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

        private static string FormatRelation(string name, Assembly dep)
        {
            return "\t" + name + " -> " + FormatModuleName(dep) + ";\n";
        }

        private static string FormatModuleName(Assembly module)
        {
            var assemblyName = module.GetName().Name;
            var postfix = assemblyName.Replace("Pincasso.", "");
            return postfix == "Core" ? "Core" : postfix.Replace(".Core", "");
        }
    }
}