using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class EnumRepresentation
  {
    public IReadOnlyList<ITypeMemberDeclaration> GetMemberDeclarations() => EnumCases;
    public override PartKind TypePartKind => PartKind.Enum;
  }
}
