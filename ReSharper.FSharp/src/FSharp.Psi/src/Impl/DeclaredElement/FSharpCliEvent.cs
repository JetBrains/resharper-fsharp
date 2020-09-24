using System.Linq;
using FSharp.Compiler.SourceCodeServices;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Impl.Special;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal class FSharpCliEvent<TDeclaration> : FSharpMemberBase<TDeclaration>, IEvent
    where TDeclaration : IFSharpDeclaration, IModifiersOwnerDeclaration, ITypeMemberDeclaration
  {
    public FSharpCliEvent([NotNull] ITypeMemberDeclaration declaration) : base(declaration)
    {
    }

    protected override FSharpSymbol GetActualSymbol(FSharpSymbol symbol) =>
      (symbol as FSharpMemberOrFunctionOrValue)?.AccessorProperty?.Value?.EventForFSharpProperty?.Value;

    public override DeclaredElementType GetElementType() =>
      CLRDeclaredElementType.EVENT;

    public IType Type => GetType(MfvType);
    public override IType ReturnType => Type;

    protected virtual FSharpType MfvType => Mfv?.FullType;

    public IAccessor Adder => new ImplicitAccessor(this, AccessorKind.ADDER);
    public IAccessor Remover => new ImplicitAccessor(this, AccessorKind.REMOVER);
    public IAccessor Raiser => null;

    public bool IsFieldLikeEvent => false;
  }

  internal class AbstractFSharpCliEvent : FSharpCliEvent<AbstractMemberDeclaration>
  {
    public AbstractFSharpCliEvent([NotNull] ITypeMemberDeclaration declaration) : base(declaration)
    {
    }

    protected override FSharpType MfvType =>
      Mfv?.CurriedParameterGroups.FirstOrDefault()?.FirstOrDefault() is { } parameter
        ? parameter.Type
        : null;

    protected override FSharpSymbol GetActualSymbol(FSharpSymbol symbol) => symbol;
  }
}
