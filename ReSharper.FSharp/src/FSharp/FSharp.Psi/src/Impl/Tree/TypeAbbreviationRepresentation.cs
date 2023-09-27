using System.Collections.Generic;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class TypeAbbreviationRepresentation
  {
    public bool CanBeUnionCase =>
      AbbreviatedTypeOrUnionCase.CanBeUnionCase;

    public IReadOnlyList<ITypeMemberDeclaration> GetMemberDeclarations() =>
      new[] {AbbreviatedTypeOrUnionCase};
  }
}
