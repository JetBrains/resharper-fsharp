using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  internal class FSharpNestedTypeUnionCase : FSharpClass, IUnionCase
  {
    public FSharpNestedTypeUnionCase([NotNull] IClassPart part) : base(part)
    {
    }

    public IEnumerable<FSharpUnionCaseField<UnionCaseFieldDeclaration>> CaseFields =>
      EnumerateParts<UnionCasePart, FSharpUnionCaseField<UnionCaseFieldDeclaration>>(part => part.CaseFields);

    public AccessRights RepresentationAccessRights =>
      GetContainingType().GetFSharpRepresentationAccessRights();
  }
}
