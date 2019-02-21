using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Impl.Special;
using JetBrains.ReSharper.Psi.Tree;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal class FSharpCliEvent<TDeclaration> : FSharpMemberBase<TDeclaration>, IEvent
    where TDeclaration : IFSharpDeclaration, IModifiersOwnerDeclaration, ITypeMemberDeclaration
  {
    public FSharpCliEvent([NotNull] ITypeMemberDeclaration declaration, FSharpMemberOrFunctionOrValue mfv)
      : base(declaration, mfv)
    {
    }

    protected override FSharpSymbol GetActualSymbol(FSharpSymbol symbol) =>
      (symbol as FSharpMemberOrFunctionOrValue)?.AccessorProperty?.Value?.EventForFSharpProperty?.Value;

    public override DeclaredElementType GetElementType() =>
      CLRDeclaredElementType.EVENT;

    public IType Type => GetType(Mfv?.FullType);
    public override IType ReturnType => Type;

    public IAccessor Adder => new ImplicitAccessor(this, AccessorKind.ADDER);
    public IAccessor Remover => new ImplicitAccessor(this, AccessorKind.REMOVER);
    public IAccessor Raiser => null;

    public bool IsFieldLikeEvent => false;
  }
}
