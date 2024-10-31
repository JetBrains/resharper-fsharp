using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public interface IParameterOwnerMemberDeclaration : IFSharpTreeNode
  {
    /// Returns associated '=' token, possibly from containing type declaration
    ITokenNode EqualsToken { get; }
    IDeclaredElement DeclaredElement { get; }
    TreeNodeEnumerable<IParametersPatternDeclaration> ParametersDeclarationsEnumerable { get; }
    TreeNodeCollection<IFSharpPattern> ParameterPatterns { get; }
  }
}
