using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IFSharpPattern : IFSharpParameterDeclaration, IConstantValueOwner
  {
    /// In simple cases uses syntax to determine whether this pattern uses an existing symbol or introduces a new one.
    /// In complex cases uses resolve via FCS.
    bool IsDeclaration { get; }

    IEnumerable<IFSharpDeclaration> Declarations { get; }
    IEnumerable<IFSharpPattern> NestedPatterns { get; }

    TreeNodeCollection<IAttribute> Attributes { get; }

    [NotNull]
    IType GetPatternType();
  }
}
