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

  internal class FSharpHiddenUnionCaseProperty : FSharpUnionCasePropertyBase<INestedTypeUnionCaseDeclaration>,
    IUnionCaseWithFields
  {
    internal FSharpHiddenUnionCaseProperty([NotNull] ITypeMemberDeclaration declaration) : base(declaration)
    {
    }

    public override AccessRights GetAccessRights() => 
      AccessRights.PRIVATE;

    public IList<IUnionCaseField> CaseFields =>
      GetDeclaration()?.Fields.Select(d => (IUnionCaseField) d.DeclaredElement).ToIList();

    public FSharpNestedTypeUnionCase NestedType =>
      GetDeclaration()?.NestedType;

    public IParametersOwner GetConstructor() =>
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
  }
}
