using System.Collections.Generic;
using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal class FSharpProperty<TDeclaration> : FSharpPropertyMemberBase<TDeclaration>, IFSharpProperty
    where TDeclaration : IFSharpDeclaration, IModifiersOwnerDeclaration, ITypeMemberDeclaration
  {
    public FSharpProperty([NotNull] ITypeMemberDeclaration declaration,
      [NotNull] FSharpMemberOrFunctionOrValue mfv) : base(declaration, mfv)
    {
    }

    public override bool IsStatic => GetContainingType() is IFSharpModule || base.IsStatic;

    public AccessRights RepresentationAccessRights => GetAccessRights();

    public bool HasExplicitAccessors => false;
    public IEnumerable<IFSharpExplicitAccessor> GetExplicitAccessors() => EmptyList<IFSharpExplicitAccessor>.Instance;

    public IEnumerable<IFSharpExplicitAccessor> FSharpExplicitGetters => EmptyList<IFSharpExplicitAccessor>.Instance;
    public IEnumerable<IFSharpExplicitAccessor> FSharpExplicitSetters => EmptyList<IFSharpExplicitAccessor>.Instance;
  }
}
