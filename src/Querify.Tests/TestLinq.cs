using Querify.Languages.Linq.Workspaces;
using Quill.Code.CSharp;
using Quill.Code.CSharp.Workspaces;

namespace Querify.Tests;

/// <summary>Workspace et services Roslyn partages par les tests : la composition MEF est couteuse.</summary>
internal static class TestLinq
{
    public static LinqWorkspace Workspace { get; } = new(new CSharpWorkspace());

    public static CSharpLanguageService Language { get; } = new();
}
