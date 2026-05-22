using System.Collections.Generic;
using System.Linq;
using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal class FSharpMethod<TDeclaration>([NotNull] ITypeMemberDeclaration declaration)
    : FSharpMethodBase<TDeclaration>(declaration), IFSharpMethod
    where TDeclaration : IFSharpDeclaration, IModifiersOwnerDeclaration, ITypeMemberDeclaration
  {
    public override bool IsStatic => GetContainingType() is IFSharpModule || base.IsStatic;
  }

  internal class FSharpTypePrivateMethod([NotNull] ITypeMemberDeclaration declaration)
    : FSharpMethodBase<FSharpProperTypeMemberDeclarationBase>(declaration), ITypePrivateMember,
      ITopLevelPatternDeclaredElement
  {
    public override AccessRights GetAccessRights() => AccessRights.INTERNAL;

    public override bool IsStatic =>
      GetDeclaration() is { IsStatic: true };

    protected override IList<FSharpGenericParameter> MfvTypeParameters =>
      GetDeclaration() is { } decl && decl.GetContainingTypeDeclaration()?.GetFcsSymbol() is FSharpEntity fcsEntity
        ? fcsEntity.GenericParameters.Concat(base.MfvTypeParameters).ToList()
        : base.MfvTypeParameters;

    public override DeclaredElementType FSharpElementType => FSharpDeclaredElementType.Function;
  }
}
