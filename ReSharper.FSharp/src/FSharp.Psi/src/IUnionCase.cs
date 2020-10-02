using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi
{
  public interface IUnionCase : ITypeMember, IRepresentationAccessRightsOwner, IFSharpDeclaredElement,
    IGeneratedConstructorOwner
  {
    bool HasFields { get; }
    IList<IUnionCaseField> CaseFields { get; }
    [CanBeNull] FSharpNestedTypeUnionCase NestedType { get; }
  }
}
