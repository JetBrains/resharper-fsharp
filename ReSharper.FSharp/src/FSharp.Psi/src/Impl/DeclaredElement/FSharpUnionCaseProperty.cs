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
  internal class FSharpUnionCaseProperty : FSharpCompiledPropertyBase<IUnionCaseLikeDeclaration>, IUnionCase
  {
    internal FSharpUnionCaseProperty([NotNull] ITypeMemberDeclaration declaration) : base(declaration)
    {
    }

    public override AccessRights GetAccessRights() =>
      HasFields ? AccessRights.PRIVATE : GetContainingType().GetRepresentationAccessRights();

    public AccessRights RepresentationAccessRights =>
      GetContainingType().GetFSharpRepresentationAccessRights();

    public bool HasFields => GetDeclaration()?.HasFields ?? false;

    public IList<IUnionCaseField> CaseFields =>
      HasFields
        ? GetDeclaration()?.Fields.Select(d => (IUnionCaseField) d.DeclaredElement).ToIList()
        : EmptyList<IUnionCaseField>.Instance;

    public FSharpUnionCaseClass NestedType =>
      GetDeclaration()?.NestedType;

    public IParametersOwner GetConstructor() =>
      HasFields ? new FSharpUnionCaseNewMethod(this) : null;

    public override bool IsStatic => true;

    public override IType ReturnType =>
      GetContainingType() is { } containingType
        ? TypeFactory.CreateType(containingType)
        : TypeFactory.CreateUnknownType(Module);
  }
}
