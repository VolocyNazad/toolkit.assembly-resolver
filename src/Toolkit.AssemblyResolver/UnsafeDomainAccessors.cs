#if NET8_0_OR_GREATER
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace Toolkit.AssemblyResolver;

internal static class UnsafeDomainAccessors
{
	[UnsafeAccessor(UnsafeAccessorKind.StaticField, Name = "AssemblyResolve")]
	internal static extern ref ResolveEventHandler? GetAssemblyResolveField(AssemblyLoadContext context);
}
#endif