using System.Collections.Generic;
using System.Collections.Immutable;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class IlAssemblyRepresentation
  {
    public IReadOnlyList<ITypeMemberDeclaration> GetMemberDeclarations() =>
      ImmutableList<ITypeMemberDeclaration>.Empty;
  }
}
