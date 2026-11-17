using System.IO;
using System.Reflection;
#if !NET8_0_OR_GREATER && NET
using System.Runtime.Loader;
#endif

namespace Toolkit.AssemblyResolver;

/// <summary>
/// Provides disposable assembly resolution scopes that resolve assemblies from a stack of module
/// directories and preserve the existing assembly resolution handlers.
/// </summary>
public static class ResolveHelper
{
    private static readonly object ResolveLock = new();
    private static readonly Stack<string> ModuleDirectories = new();
    private static object? _domainResolvers;

    /// <summary>
    /// Begins an assembly resolution scope that searches the directory of the module that declares
    /// <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">A type whose module directory is added to the resolution stack.</typeparam>
    /// <returns>A scope that restores the previous resolution state when it is disposed.</returns>
    public static IDisposable BeginAssemblyResolveScope<T>()
        => BeginAssemblyResolveScope(typeof(T));

    /// <summary>
    /// Begins an assembly resolution scope that searches the directory of the module that declares
    /// <paramref name="type"/>.
    /// </summary>
    /// <param name="type">A type whose module directory is added to the resolution stack.</param>
    /// <returns>
    /// A scope that restores the previous resolution state when it is disposed, or an inert scope
    /// when the module has no file on disk.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> is <see langword="null"/>.</exception>
    public static IDisposable BeginAssemblyResolveScope(Type type)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(type);
#else
        if (type is null) throw new ArgumentNullException(nameof(type));
#endif

        if (type.Module.FullyQualifiedName == "<Unknown>")
            return DisposedAssemblyResolveScope.Instance;

        var moduleDirectory = Path.GetDirectoryName(type.Module.FullyQualifiedName);
        if (moduleDirectory is null)
            return DisposedAssemblyResolveScope.Instance;

        return BeginAssemblyResolveScope(moduleDirectory);
    }

    /// <summary>
    /// Begins an assembly resolution scope that searches <paramref name="directory"/> for assemblies
    /// that the default resolution cannot find.
    /// </summary>
    /// <param name="directory">The directory added to the resolution stack.</param>
    /// <returns>
    /// A scope that removes the directory from the stack when it is disposed, or an inert scope when
    /// <paramref name="directory"/> is empty.
    /// </returns>
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