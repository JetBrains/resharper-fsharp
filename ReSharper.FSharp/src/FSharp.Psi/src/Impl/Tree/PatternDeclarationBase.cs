using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Common.Util;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class NamedPat
  {
    protected override string DeclaredElementName => Identifier.GetCompiledName(Attributes);
    public override TreeTextRange GetNameRange() => Identifier.GetNameRange();

    public override IFSharpIdentifier NameIdentifier => Identifier;
  }

  internal partial class LongIdentPat
  {
    protected override string DeclaredElementName => Identifier.GetCompiledName(Attributes);
    public override string SourceName => IsDeclaration ? base.SourceName : SharedImplUtil.MISSING_DECLARATION_NAME;
    public override TreeTextRange GetNameRange() => IsDeclaration ? base.GetNameRange() : TreeTextRange.InvalidRange;

    public override IFSharpIdentifier NameIdentifier => Identifier;

    protected override IDeclaredElement CreateDeclaredElement() =>
      IsDeclaration ? base.CreateDeclaredElement() : null;
  }
  
  internal abstract class PatternDeclarationBase : FSharpProperTypeMemberDeclarationBase, IFunctionDeclaration
  {
    IFunction IFunctionDeclaration.DeclaredElement => base.DeclaredElement as IFunction;

    protected override IDeclaredElement CreateDeclaredElement()
    {
      if (!(GetFSharpSymbol() is FSharpMemberOrFunctionOrValue mfv)) return null;

      if (mfv.LiteralValue != null)
        return new FSharpLiteral(this, mfv);

      if (!mfv.IsValCompiledAsMethod())
        return new ModuleValue(this, mfv);

      return !mfv.IsInstanceMember && mfv.CompiledName.StartsWith("op_", StringComparison.Ordinal)
        ? (IDeclaredElement) new FSharpSignOperator<PatternDeclarationBase>(this, mfv, null)
        : new ModuleFunction(this, mfv, null);
    }

    public TreeNodeCollection<IFSharpAttribute> Attributes =>
      GetBinding()?.Attributes ??
      TreeNodeCollection<IFSharpAttribute>.Empty;

    public TreeNodeEnumerable<IFSharpAttribute> AttributesEnumerable =>
      GetBinding()?.AttributesEnumerable ?? 
      TreeNodeEnumerable<IFSharpAttribute>.Empty;

    [CanBeNull]
    protected IBinding GetBinding()
    {
      ITreeNode node = this;
      while (node != null)
      {
        switch (node)
        {
          case IBinding binding:
            return binding;
          case LongIdentPat longIdentPat when !longIdentPat.IsDeclaration:
            return null;
          default:
            node = node.Parent;
            break;
        }
      }

      return null;
    }
  }

  internal abstract class SynPatternBase : FSharpCompositeElement
  {
    public virtual bool IsDeclaration => false;

    public virtual IEnumerable<ITypeMemberDeclaration> Declarations =>
      EmptyList<ITypeMemberDeclaration>.Instance;
    
    public TreeNodeCollection<IFSharpAttribute> Attributes => TreeNodeCollection<IFSharpAttribute>.Empty;
    public TreeNodeEnumerable<IFSharpAttribute> AttributesEnumerable => TreeNodeEnumerable<IFSharpAttribute>.Empty;
  }
}
