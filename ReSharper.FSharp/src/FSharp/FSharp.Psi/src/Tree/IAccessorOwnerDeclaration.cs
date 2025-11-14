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
  public static IAccessorDeclaration Getter(this IAccessorOwnerDeclaration owner) =>
    owner.AccessorDeclarationsEnumerable.FirstOrDefault(x => x.Kind == AccessorKind.GETTER);

  public static IAccessorDeclaration Setter(this IAccessorOwnerDeclaration owner) =>
    owner.AccessorDeclarationsEnumerable.FirstOrDefault(x => x.Kind == AccessorKind.SETTER);
}
