using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal abstract class ObjectModelTypeRepresentationBase : TypeRepresentationBase
  {
    public IReadOnlyList<ITypeMemberDeclaration> GetMemberDeclarations() =>
      ((IObjectModelTypeRepresentation) this).TypeMembers.OfType<ITypeMemberDeclaration>().AsIReadOnlyList();
  }
}
