using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace DependencyGrapher
{
    public class DotModuleDiagram : IDisposable
    {
        private readonly DependencyDiagramOptions options;
        private readonly string rootAssemblyPath;
        private readonly string assemblyFolder;
        private readonly Regex assemblyIncludeRegex;
        private readonly Regex assemblyExcludeRegex;
        private readonly Regex interfaceExcludeRegex;

        private readonly Dictionary<Assembly, Assembly[]> moduleMap =
            new Dictionary<Assembly, Assembly[]>();

        public DotModuleDiagram(string rootAssemblyPath, DependencyDiagramOptions options = null)
        {
            this.options = options ?? new DependencyDiagramOptions();
            this.rootAssemblyPath = rootAssemblyPath;

            assemblyFolder = GetAssemblyFolder(rootAssemblyPath);

            if (!string.IsNullOrEmpty(this.options.AssemblyIncludeRegex))
                assemblyIncludeRegex = new Regex(this.options.AssemblyIncludeRegex);

            if (!string.IsNullOrEmpty(this.options.AssemblyExcludeRegex))
                assemblyExcludeRegex = new Regex(this.options.AssemblyExcludeRegex);

            if (!string.IsNullOrEmpty(this.options.InterfaceIncludeRegex))
                interfaceExcludeRegex = new Regex(this.options.InterfaceIncludeRegex);

            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += OnReflectionOnlyAssemblyResolve;
        }

        private static string GetAssemblyFolder(string path)
        {
            var idx = path.LastIndexOf(Path.DirectorySeparatorChar);
            return idx < 0 ? "" : path.Substring(0, idx + 1);
        }

        public string Draw()
        {
            FindDependencies(Assembly.ReflectionOnlyLoadFrom(rootAssemblyPath));

            if (options.HideTransitiveReferences)
                RemoveTransitiveReferences();

            return DrawModuleMap();
        }

        private void FindDependencies(Assembly module)
        {
            var name = module.GetName().Name;
            if (moduleMap.Keys.Any(o => o.GetName().Name == name))
                return;

            var dependencies = module
                .GetReferencedAssemblies()
                .Where(o => IsModule(o.Name))
                .Select(o => o.Name)
                .OrderBy(o => o)
                .Select(LoadAssembly)
                .ToArray();

            if (IsModule(name))
                moduleMap[module] = dependencies;

            foreach (var dep in dependencies)
            {
                FindDependencies(dep);
            }
        }

        private Assembly OnReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
        {
            return LoadAssembly(args.Name);
        }

        private Assembly LoadAssembly(string name)
        {
            var assemblyFile = assemblyFolder + GetAssemblyFileName(name);
            return File.Exists(assemblyFile)
                ? Assembly.ReflectionOnlyLoadFrom(assemblyFile)
                : Assembly.ReflectionOnlyLoad(name);
        }

        private static string GetAssemblyFileName(string fullName)
        {
            var idx = fullName.IndexOf(',');
            return (idx < 0 ? fullName : fullName.Substring(0, idx)) + ".dll";
        }

        private bool IsModule(string moduleName)
        {
            return assemblyIncludeRegex.IsMatch(moduleName) &&
                   !assemblyExcludeRegex.IsMatch(moduleName);
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

        private IEnumerable<string> GetDomainObjects(Assembly module)
        {
            return module.GetTypes()
                .Where(IsDomainObject)
                .OrderBy(o => o.Name)
                .Select(o => o.Name)
                .DefaultIfEmpty();
        }

        private bool IsDomainObject(Type type)
        {
            if (!type.IsClass)
                return false;

            if (interfaceExcludeRegex == null)
                return false;

            return type.GetInterfaces().Any(o => interfaceExcludeRegex.IsMatch(o.Name));
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

        public void Dispose()
        {
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve -= OnReflectionOnlyAssemblyResolve;
        }
    }
}