using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public interface IParameterOwnerMemberDeclaration : IFSharpTreeNode
  {
    /// Returns associated '=' token, possibly from containing type declaration
    JetBrains.ReSharper.Psi.Tree.ITokenNode EqualsToken { get; }
    JetBrains.ReSharper.Psi.Tree.TreeNodeCollection<IFSharpPattern> ParameterPatterns { get; }
    IDeclaredElement DeclaredElement { get; }
  }
}
