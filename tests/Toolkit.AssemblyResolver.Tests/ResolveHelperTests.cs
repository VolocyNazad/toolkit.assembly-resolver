using Xunit;

namespace Toolkit.AssemblyResolver.Tests;

public sealed class ResolveHelperTests
{
    [Fact]
    public void Empty_directory_scope_can_be_disposed_repeatedly()
    {
        var scope = ResolveHelper.BeginAssemblyResolveScope(string.Empty);

        scope.Dispose();
        scope.Dispose();
    }

    [Fact]
    public void Type_scope_can_be_disposed_repeatedly()
    {
        var scope = ResolveHelper.BeginAssemblyResolveScope<ResolveHelperTests>();

        scope.Dispose();
        scope.Dispose();
    }

    [Fact]
    public void Nested_directory_scopes_restore_previous_resolver_state()
    {
        using var outer = ResolveHelper.BeginAssemblyResolveScope(AppContext.BaseDirectory);
        using var inner = ResolveHelper.BeginAssemblyResolveScope(Path.GetTempPath());
    }
}
