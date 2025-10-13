using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi
{
  public interface IFSharpUnionCase : IFSharpGeneratedConstructorOwnerPart, IFSharpRepresentationAccessRightsOwner,
    IFSharpDeclaredElement, ITypeMember
  {
    bool HasFields { get; }
    IList<IUnionCaseField> CaseFields { get; }
    [CanBeNull] FSharpUnionCaseClass NestedType { get; }
  }
}
