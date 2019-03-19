using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal class FSharpConversionOperator<TDeclaration> : FSharpOperatorBase<TDeclaration>, IConversionOperator
    where TDeclaration : IFSharpDeclaration, IModifiersOwnerDeclaration, ITypeMemberDeclaration
  {
    internal FSharpConversionOperator([NotNull] ITypeMemberDeclaration declaration,
      [NotNull] FSharpMemberOrFunctionOrValue mfv, bool isExplicitCast) : base(declaration, mfv) =>
      IsExplicitCast = isExplicitCast;

    public override DeclaredElementType GetElementType() =>
      CLRDeclaredElementType.CONVERSION_OPERATOR;

    public bool IsExplicitCast { get; }
    public bool IsImplicitCast => !IsExplicitCast;
  }

  internal class FSharpSignOperator<TDeclaration> : FSharpOperatorBase<TDeclaration>, ISignOperator
    where TDeclaration : FSharpDeclarationBase, IFSharpDeclaration, IModifiersOwnerDeclaration, ITypeMemberDeclaration
  {
    internal FSharpSignOperator([NotNull] ITypeMemberDeclaration declaration,
      [NotNull] FSharpMemberOrFunctionOrValue mfv) : base(declaration, mfv)
    {
    }

    public override DeclaredElementType GetElementType() =>
      CLRDeclaredElementType.SIGN_OPERATOR;
  }

  internal abstract class FSharpOperatorBase<TDeclaration> : FSharpTypeParametersOwnerBase<TDeclaration>, IOperator
    where TDeclaration : IFSharpDeclaration, IModifiersOwnerDeclaration, ITypeMemberDeclaration
  {
    internal FSharpOperatorBase([NotNull] ITypeMemberDeclaration declaration,
      [NotNull] FSharpMemberOrFunctionOrValue mfv) : base(declaration, mfv)
    {
    }

    public override bool IsStatic => true;
  }
}
