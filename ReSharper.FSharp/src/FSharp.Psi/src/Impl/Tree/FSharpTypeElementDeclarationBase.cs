using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal abstract class FSharpTypeElementDeclarationBase : FSharpCachedDeclarationBase, IFSharpTypeElementDeclaration
  {
    ITypeMember ITypeMemberDeclaration.DeclaredElement => (ITypeMember) DeclaredElement;
    ITypeElement ITypeDeclaration.DeclaredElement => (ITypeElement) DeclaredElement;

    /// May take long time due to waiting for FCS.
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public IEnumerable<IDeclaredType> SuperTypes => GetFSharpSymbol() is FSharpEntity entity
      ? FSharpTypesUtil.GetSuperTypes(entity, TypeParameters, GetPsiModule())
      : EmptyList<IDeclaredType>.Instance;

    /// May take long time due to waiting for FCS.
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public IDeclaredType BaseClassType => GetFSharpSymbol() is FSharpEntity entity
      ? FSharpTypesUtil.GetBaseType(entity, TypeParameters, GetPsiModule())
      : null;

    public override FSharpSymbol GetFSharpSymbol()
    {
      var symbol = base.GetFSharpSymbol();
      if (symbol is FSharpEntity || symbol is FSharpUnionCase) return symbol;

      if (!(symbol is FSharpMemberOrFunctionOrValue mfv))
        return null;

      return mfv.IsConstructor || mfv.IsImplicitConstructor ? mfv.DeclaringEntity?.Value : null;
    }

    [NotNull]
    private IList<ITypeParameter> TypeParameters => ((ITypeDeclaration) this).DeclaredElement?.TypeParameters ??
                                                    EmptyList<ITypeParameter>.Instance;

    public virtual IReadOnlyList<ITypeMemberDeclaration> MemberDeclarations
    {
      get
      {
        var members = this.Children<ITypeMemberDeclaration>();
        var implementedMembers = this.Children<IInterfaceImplementation>()
          .SelectMany(m => m.Children<ITypeMemberDeclaration>());
        var extensionMembers = this.Children<ITypeExtension>()
          .SelectMany(m => m.Children<ITypeMemberDeclaration>());
        return members.Concat(implementedMembers).Concat(extensionMembers).WhereNotNull().ToTreeNodeCollection();
      }
    }

    public string CLRName => this.MakeClrName();
    public IReadOnlyList<ITypeDeclaration> TypeDeclarations => EmptyList<ITypeDeclaration>.Instance;

    public IReadOnlyList<ITypeDeclaration> NestedTypeDeclarations =>
      MemberDeclarations.OfType<ITypeDeclaration>().ToTreeNodeCollection();
  }
}