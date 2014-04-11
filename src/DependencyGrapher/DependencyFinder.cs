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

        private readonly Dictionary<string, ModuleInfo> modules =
            new Dictionary<string, ModuleInfo>();

        public DependencyFinder(string rootAssemblyPath, DependencyFinderOptions options = null)
        {
            this.options = options ?? new DependencyFinderOptions();
            this.rootAssemblyPath = rootAssemblyPath;

            assemblyFolder = GetAssemblyFolder(rootAssemblyPath);

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
                .Where(IncludeModule)
                .Select(LoadAssembly)
                .Select(FindDependencies)
                .Where(o => o != null)
                .OrderBy(o => o.Name)
                .ToArray();

            if (IncludeModule(name))
            {
                var module = new ModuleInfo(name)
                {
                    Dependencies = dependencies,
                    Types = GetTypes(assembly).ToArray()
                };
                modules[name] = module;
                return module;
            }

            return null;
        }

        private bool IncludeModule(string moduleName)
        {
            return Regex.IsMatch(moduleName, options.AssemblyIncludeRegex ?? ".*") &&
                   !Regex.IsMatch(moduleName, options.AssemblyExcludeRegex ?? @"^System\..*");
        }

        private IEnumerable<string> GetTypes(Assembly module)
        {
            return module.GetTypes()
                .Where(IncludeType)
                .OrderBy(o => o.Name)
                .Select(o => o.Name)
                .Distinct()
                .DefaultIfEmpty();
        }

        private bool IncludeType(Type type)
        {
            if (Regex.IsMatch(type.Name, options.TypeIncludeRegex ?? "-") &&
                !Regex.IsMatch(type.Name, options.TypeExcludeRegex ?? "-"))
                return true;

            return GetParentTypes(type).Any(IncludeType);
        }

        private static IEnumerable<Type> GetParentTypes(Type type)
        {
            if (type == null || type.BaseType == null)
                yield break;

            foreach (var i in type.GetInterfaces())
                yield return i;

            var currentBaseType = type.BaseType;
            while (currentBaseType != null)
            {
                yield return currentBaseType;
                currentBaseType = currentBaseType.BaseType;
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