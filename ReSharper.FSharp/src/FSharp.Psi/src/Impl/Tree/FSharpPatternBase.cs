using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal abstract class FSharpPatternBase : FSharpCompositeElement
  {
    public virtual bool IsDeclaration => false;

    public virtual IEnumerable<IFSharpPattern> NestedPatterns =>
      EmptyList<IFSharpPattern>.Instance;

    public virtual IEnumerable<IFSharpDeclaration> Declarations =>
      NestedPatterns.OfType<IFSharpDeclaration>();

    public TreeNodeCollection<IAttribute> Attributes => TreeNodeCollection<IAttribute>.Empty;

    public virtual IType GetPatternType() => TypeFactory.CreateUnknownType(GetPsiModule());
  }
}
