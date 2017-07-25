using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
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
          ? FSharpTypesUtil.GetSuperTypes(entity, TypeParameters, GetPsiModule())
          : EmptyList<IDeclaredType>.Instance;
      }
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
          ? FSharpTypesUtil.GetBaseType(entity, TypeParameters, GetPsiModule())
          : null;
      }
    }

    public override FSharpSymbol GetFSharpSymbol()
    {
      var symbol = base.GetFSharpSymbol();
      if (symbol is FSharpEntity) return symbol;

      var mfv = symbol as FSharpMemberOrFunctionOrValue;
      if (mfv == null)
        return null;

      return mfv.IsConstructor || mfv.IsImplicitConstructor ? mfv.EnclosingEntity : null;
    }

    [NotNull]
    private IList<ITypeParameter> TypeParameters => ((ITypeDeclaration) this).DeclaredElement?.TypeParameters ??
                                                    EmptyList<ITypeParameter>.Instance;

    public virtual TreeNodeCollection<ITypeMemberDeclaration> MemberDeclarations
    {
      get
      {
        var members = this.Children<ITypeMemberDeclaration>();
        var implementedMembers = this.Children<IInterfaceImplementation>()
          .SelectMany(m => m.Children<ITypeMemberDeclaration>());
        return members.Prepend(implementedMembers).ToTreeNodeCollection();
      }
    }

    public string CLRName => FSharpImplUtil.MakeClrName(this);
    public IList<ITypeDeclaration> TypeDeclarations => EmptyList<ITypeDeclaration>.Instance;

    public TreeNodeCollection<ITypeDeclaration> NestedTypeDeclarations =>
      MemberDeclarations.OfType<ITypeDeclaration>().ToTreeNodeCollection();
  }
}