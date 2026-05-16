using System.IO;
using System.Reflection;
#if !NET8_0_OR_GREATER && NET
using System.Runtime.Loader;
#endif

namespace Toolkit.AssemblyResolver;

public static class ResolveHelper
{
    private static readonly object ResolveLock = new();
    private static readonly Stack<string> ModuleDirectories = new();
    private static object? _domainResolvers;

    public static IDisposable BeginAssemblyResolveScope<T>()
        => BeginAssemblyResolveScope(typeof(T));

    public static IDisposable BeginAssemblyResolveScope(Type type)
    {
        if (type.Module.FullyQualifiedName == "<Unknown>")
            return DisposedAssemblyResolveScope.Instance;

        var moduleDirectory = Path.GetDirectoryName(type.Module.FullyQualifiedName);
        if (moduleDirectory is null)
            return DisposedAssemblyResolveScope.Instance;

        return BeginAssemblyResolveScope(moduleDirectory);
    }

    public static IDisposable BeginAssemblyResolveScope(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            return DisposedAssemblyResolveScope.Instance;
        }

        lock (ResolveLock)
        {
            var isFirstScope = ModuleDirectories.Count == 0;
            ModuleDirectories.Push(directory);

            if (isFirstScope)
            {
                OverrideDomainResolvers();
            }
        }

        return new AssemblyResolveScope();
    }

    private static void OverrideDomainResolvers()
    {
#if NET8_0_OR_GREATER
		ref var resolversRef = ref UnsafeDomainAccessors.GetAssemblyResolveField(null!);
		var resolvers = resolversRef;
		resolversRef = null;
#elif NET
        var loadContextType = typeof(AssemblyLoadContext);
        var resolversField = loadContextType.GetField("AssemblyResolve", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)!;
        var resolvers = (ResolveEventHandler?)resolversField.GetValue(null);
        resolversField.SetValue(null, null);
#else
        var domainType = AppDomain.CurrentDomain.GetType();
        var resolversField = domainType.GetField("_AssemblyResolve", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)!;
        var resolvers = (ResolveEventHandler)resolversField.GetValue(AppDomain.CurrentDomain);
        resolversField.SetValue(AppDomain.CurrentDomain, null);
#endif

        _domainResolvers = resolvers;

        AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        AppDomain.CurrentDomain.AssemblyResolve += resolvers;
    }

    private static Assembly? OnAssemblyResolve(object? sender, ResolveEventArgs args)
    {
        string[] directories;
        lock (ResolveLock)
        {
            if (ModuleDirectories.Count == 0) return null;
            directories = ModuleDirectories.ToArray();
        }

        var assemblyName = new AssemblyName(args.Name).Name;

        foreach (var directory in directories)
        {
            var assemblyPath = Path.Combine(directory, $"{assemblyName}.dll");
            if (File.Exists(assemblyPath))
                return Assembly.LoadFrom(assemblyPath);
        }

        return null;
    }

    private static void RestoreResolvers()
    {
        if (_domainResolvers is null) return;

        AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;

#if NET8_0_OR_GREATER
		UnsafeDomainAccessors.GetAssemblyResolveField(null!) = (ResolveEventHandler?)_domainResolvers;
#elif NET
        var loadContextType = typeof(AssemblyLoadContext);
        var resolversField = loadContextType.GetField("AssemblyResolve", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)!;
        resolversField.SetValue(null, _domainResolvers);
#else
        var domainType = AppDomain.CurrentDomain.GetType();
        var resolversField = domainType.GetField("_AssemblyResolve", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)!;
        resolversField.SetValue(AppDomain.CurrentDomain, _domainResolvers);
#endif

        _domainResolvers = null;
    }

    private sealed class AssemblyResolveScope : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;

            lock (ResolveLock)
            {
                if (ModuleDirectories.Count == 0) return;
                ModuleDirectories.Pop();

                if (ModuleDirectories.Count == 0)
                    RestoreResolvers();
            }
        }
    }

    private sealed class DisposedAssemblyResolveScope : IDisposable
    {
        public static readonly DisposedAssemblyResolveScope Instance = new();

        public void Dispose() { }
    }
}