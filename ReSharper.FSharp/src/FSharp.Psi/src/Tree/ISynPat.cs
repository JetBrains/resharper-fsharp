using System.Collections.Generic;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface ISynPat
  {
    bool IsDeclaration { get; }
    IEnumerable<ITypeMemberDeclaration> Declarations { get; }

    TreeNodeCollection<IFSharpAttribute> Attributes { get; }
    TreeNodeEnumerable<IFSharpAttribute> AttributesEnumerable { get; }
  }
}
