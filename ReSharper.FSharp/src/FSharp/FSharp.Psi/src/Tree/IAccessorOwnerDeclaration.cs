using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

using JetBrains.ReSharper.Psi.Tree;

public interface IAccessorOwnerDeclaration
{
  TreeNodeCollection<IAccessorDeclaration> AccessorDeclarations { get; }
  TreeNodeEnumerable<IAccessorDeclaration> AccessorDeclarationsEnumerable { get; }
}

public static class AccessorOwnerExtensions
{
  public static IAccessorDeclaration GetAccessor(this IAccessorOwnerDeclaration owner, AccessorKind kind) =>
    owner.AccessorDeclarationsEnumerable.FirstOrDefault(x => x.Kind == kind);
}
