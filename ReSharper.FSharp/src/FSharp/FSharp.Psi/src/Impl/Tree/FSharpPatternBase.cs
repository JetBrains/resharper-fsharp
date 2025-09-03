using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal abstract class FSharpPatternBase : FSharpCompositeElement, IFSharpPattern
  {
    public virtual bool IsDeclaration => false;

    public virtual IEnumerable<IFSharpPattern> NestedPatterns =>
      EmptyList<IFSharpPattern>.Instance;

    public virtual IEnumerable<IFSharpDeclaration> Declarations =>
      NestedPatterns.OfType<IFSharpDeclaration>();

    public TreeNodeCollection<IAttribute> Attributes => TreeNodeCollection<IAttribute>.Empty;

    public virtual IType GetPatternType() => TypeFactory.CreateUnknownType(GetPsiModule());

    public virtual ConstantValue ConstantValue => ConstantValue.NOT_COMPILE_TIME_CONSTANT;

    public IFSharpIdentifier NameIdentifier => this.TryGetNameIdentifierOwner()?.Identifier;
    public IFSharpParameter FSharpParameter => this.TryGetDeclaredFSharpParameter();
  }
}
