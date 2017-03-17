using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.FSharp.Util;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  internal abstract class FSharpTypeElementDeclarationBase : FSharpCachedDeclarationBase, IFSharpTypeElementDeclaration
  {
    ITypeMember ITypeMemberDeclaration.DeclaredElement => (ITypeMember) DeclaredElement;
    ITypeElement ITypeDeclaration.DeclaredElement => (ITypeElement) DeclaredElement;

    /// <summary>
    /// May take long time due to waiting for FCS
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public IEnumerable<IDeclaredType> SuperTypes
    {
      get
      {
        var entity = GetFSharpSymbol() as FSharpEntity;
        return entity != null
          ? FSharpElementsUtil.GetSuperTypes(entity, GetPsiModule())
          : EmptyList<IDeclaredType>.Instance;
      }
    }

    public override FSharpSymbol GetFSharpSymbol()
    {
      var symbol = base.GetFSharpSymbol();
      var mfv = symbol as FSharpMemberOrFunctionOrValue;
      Symbol = mfv != null && mfv.IsImplicitConstructor
        ? mfv.EnclosingEntity
        : symbol;

      return Symbol;
    }

    /// <summary>
    /// May take long time due to waiting for FCS
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public IDeclaredType BaseClassType
    {
      get
      {
        var entity = GetFSharpSymbol() as FSharpEntity;
        return entity != null
          ? FSharpElementsUtil.GetBaseType(entity, GetPsiModule())
          : null;
      }
    }

    public virtual TreeNodeCollection<ITypeMemberDeclaration> MemberDeclarations =>
      this.Children<ITypeMemberDeclaration>().ToTreeNodeCollection(); // todo: hide non compiled types

    public string CLRName => FSharpImplUtil.MakeClrName(this);
    public IList<ITypeDeclaration> TypeDeclarations => EmptyList<ITypeDeclaration>.Instance;

    public TreeNodeCollection<ITypeDeclaration> NestedTypeDeclarations =>
      MemberDeclarations.OfType<ITypeDeclaration>().ToTreeNodeCollection();
  }
}