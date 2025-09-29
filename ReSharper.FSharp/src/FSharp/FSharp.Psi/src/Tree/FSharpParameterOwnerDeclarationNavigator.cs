using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

public class FSharpParameterOwnerDeclarationNavigator
{
  [CanBeNull]
  public static IFSharpParameterOwnerDeclaration Unwrap([CanBeNull] ITreeNode treeNode) =>
    treeNode switch
    {
      IReferencePat refPat => refPat.Binding,
      IFSharpParameterOwnerDeclaration paramOwnerDecl => paramOwnerDecl,
      _ => null
    };
}
