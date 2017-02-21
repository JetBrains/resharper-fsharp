using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Psi.FSharp.Tree
{
  public partial interface ILongIdentifier
  {
    TreeNodeCollection<ITokenNode> Qualifiers { get; }
    string QualifiedName { get; }
    string ShortName { get; }
  }
}