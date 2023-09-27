using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public interface IParameterOwnerMemberDeclaration : IFSharpTreeNode
  {
    JetBrains.ReSharper.Psi.Tree.TreeNodeCollection<IFSharpPattern> ParameterPatterns { get; }
    IDeclaredElement DeclaredElement { get; }
  }
}
