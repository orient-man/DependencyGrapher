using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace DependencyGrapher
{
    public class DependencyFinder : IDisposable
    {
        private readonly DependencyFinderOptions options;
        private readonly string rootAssemblyPath;
        private readonly string assemblyFolder;
        private readonly Regex assemblyIncludeRegex;
        private readonly Regex assemblyExcludeRegex;
        private readonly Regex interfaceExcludeRegex;

        private readonly Dictionary<string, ModuleInfo> modules =
            new Dictionary<string, ModuleInfo>();

        public DependencyFinder(string rootAssemblyPath, DependencyFinderOptions options = null)
        {
            this.options = options ?? new DependencyFinderOptions();
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

        public IEnumerable<ModuleInfo> FindDependencies()
        {
            FindDependencies(Assembly.ReflectionOnlyLoadFrom(rootAssemblyPath));

            if (options.RemoveTransitiveReferences)
                RemoveTransitiveReferences();

            return modules.Values;
        }

        private ModuleInfo FindDependencies(Assembly assembly)
        {
            var name = assembly.GetName().Name;

            if (modules.ContainsKey(name))
                return modules[name];

            var dependencies = assembly
                .GetReferencedAssemblies()
                .Select(o => o.Name)
                .Where(IsModule)
                .Select(LoadAssembly)
                .Select(FindDependencies)
                .Where(o => o != null)
                .OrderBy(o => o.Name)
                .ToArray();

            if (IsModule(name))
            {
                var module = new ModuleInfo(name)
                {
                    Dependencies = dependencies,
                    DomainObjects = GetDomainObjects(assembly).ToArray()
                };
                modules[name] = module;
                return module;
            }

            return null;
        }

        private bool IsModule(string moduleName)
        {
            var include = true;
            if (assemblyIncludeRegex != null)
                include = assemblyIncludeRegex.IsMatch(moduleName);

            if (!include)
                return false;

            return assemblyExcludeRegex == null || !assemblyExcludeRegex.IsMatch(moduleName);
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

        private void RemoveTransitiveReferences()
        {
            foreach (var module in modules.Keys.ToArray())
            {
                var dependencies = modules[module].Dependencies;
                modules[module].Dependencies =
                    dependencies
                        .Where(
                            o =>
                                dependencies
                                    .SelectMany(GetAllDependencies).ToList()
                                    .All(d => d != o))
                        .ToArray();
            }
        }

        private IEnumerable<ModuleInfo> GetAllDependencies(ModuleInfo module)
        {
            return modules[module.Name].Dependencies
                .Concat(modules[module.Name].Dependencies.SelectMany(GetAllDependencies));
        }

        public void Dispose()
        {
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve -= OnReflectionOnlyAssemblyResolve;
        }
    }
}