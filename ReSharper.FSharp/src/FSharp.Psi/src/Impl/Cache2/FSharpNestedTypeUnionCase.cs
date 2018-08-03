using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  internal class FSharpNestedTypeUnionCase : FSharpClass, IUnionCase
  {
    public FSharpNestedTypeUnionCase([NotNull] IClassPart part) : base(part)
    {
    }

    public IEnumerable<FSharpFieldProperty> CaseFields =>
      EnumerateParts<UnionCasePart, FSharpFieldProperty>(part => part.CaseFields);

    public AccessRights RepresentationAccessRights =>
      GetContainingType() is TypeElement typeElement
        ? typeElement.GetFSharpRepresentationAccessRights()
        : AccessRights.PUBLIC;
  }
}
