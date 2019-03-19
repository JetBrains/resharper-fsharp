using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface ILongIdentifier : IFSharpIdentifier
  {
    TreeNodeCollection<ITokenNode> Qualifiers { get; }
    string QualifiedName { get; }
  }
}