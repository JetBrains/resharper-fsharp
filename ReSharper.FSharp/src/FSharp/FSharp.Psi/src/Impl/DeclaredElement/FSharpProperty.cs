using System.Collections.Generic;
using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal class FSharpProperty<TDeclaration>(
    [NotNull] ITypeMemberDeclaration declaration,
    [NotNull] FSharpMemberOrFunctionOrValue mfv)
    : FSharpPropertyMemberBase<TDeclaration>(declaration, mfv), IFSharpProperty
    where TDeclaration : IFSharpDeclaration, IModifiersOwnerDeclaration, ITypeMemberDeclaration
  {
    public override bool IsStatic => GetContainingType() is IFSharpModule || base.IsStatic;

    public AccessRights RepresentationAccessRights => GetAccessRights();

    public bool IsIndexerLike => false;

    public IEnumerable<IMethod> Accessors
    {
      get
      {
        if (Getter is {} getter) yield return getter;
        if (Setter is {} setter) yield return setter;
      }
    }
  }
}
