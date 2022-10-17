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
  internal class FSharpMethod<TDeclaration> : FSharpMethodBase<TDeclaration>, IFSharpMethod
    where TDeclaration : IFSharpDeclaration, IModifiersOwnerDeclaration, ITypeMemberDeclaration
  {
    public FSharpMethod([NotNull] ITypeMemberDeclaration declaration) : base(declaration)
    {
    }

    public override bool IsStatic => GetContainingType() is IFSharpModule || base.IsStatic;
  }

  internal class FSharpTypePrivateMethod : FSharpMethodBase<FSharpProperTypeMemberDeclarationBase>, ITypePrivateMember,
    ITopLevelPatternDeclaredElement
  {
    public FSharpTypePrivateMethod([NotNull] ITypeMemberDeclaration declaration) : base(declaration)
    {
    }

    public override AccessRights GetAccessRights() => AccessRights.INTERNAL;

    public override bool IsStatic =>
      GetDeclaration() is { IsStatic: true };

    protected override IList<FSharpGenericParameter> MfvTypeParameters =>
      GetDeclaration() is { } decl && decl.GetContainingTypeDeclaration()?.GetFcsSymbol() is FSharpEntity fcsEntity
        ? fcsEntity.GenericParameters.Concat(base.MfvTypeParameters).ToList()
        : base.MfvTypeParameters;

    internal override IFSharpParameterOwnerDeclaration ParameterOwnerDeclaration =>
      BindingNavigator.GetByHeadPattern((IFSharpPattern)GetDeclaration());
  }
}
