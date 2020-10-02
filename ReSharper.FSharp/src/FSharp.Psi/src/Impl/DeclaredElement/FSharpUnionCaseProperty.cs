using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  /// A union case compiled to a static property.
  internal class FSharpUnionCaseProperty : FSharpUnionCasePropertyBase<IUnionCaseDeclaration>
  {
    public FSharpUnionCaseProperty([NotNull] ITypeMemberDeclaration declaration) : base(declaration)
    {
    }
  }

  internal class FSharpHiddenUnionCaseProperty : FSharpUnionCasePropertyBase<INestedTypeUnionCaseDeclaration>
  {
    internal FSharpHiddenUnionCaseProperty([NotNull] ITypeMemberDeclaration declaration) : base(declaration)
    {
    }

    public override AccessRights GetAccessRights() =>
      AccessRights.PRIVATE;

    public override bool HasFields => true;

    public override IList<IUnionCaseField> CaseFields =>
      GetDeclaration()?.Fields.Select(d => (IUnionCaseField) d.DeclaredElement).ToIList();

    public override FSharpNestedTypeUnionCase NestedType =>
      GetDeclaration()?.NestedType;

    public override IParametersOwner GetConstructor() =>
      new NewUnionCaseMethod(this);
  }

  internal class FSharpUnionCasePropertyBase<T> : FSharpCompiledPropertyBase<T>, IUnionCase
    where T : IUnionCaseDeclaration
  {
    internal FSharpUnionCasePropertyBase([NotNull] ITypeMemberDeclaration declaration) : base(declaration)
    {
    }

    public override AccessRights GetAccessRights() => GetContainingType().GetRepresentationAccessRights();
    public AccessRights RepresentationAccessRights => GetContainingType().GetFSharpRepresentationAccessRights();

    public override bool IsStatic => true;

    public override IType ReturnType =>
      GetContainingType() is var containingType && containingType != null
        ? TypeFactory.CreateType(containingType)
        : TypeFactory.CreateUnknownType(Module);

    public virtual bool HasFields => false;
    public virtual IList<IUnionCaseField> CaseFields => EmptyList<IUnionCaseField>.Instance;
    public virtual FSharpNestedTypeUnionCase NestedType => null;

    public virtual IParametersOwner GetConstructor() => null;
  }
}
