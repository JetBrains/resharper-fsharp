using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal class FSharpConversionOperator<TDeclaration> : FSharpOperatorBase<TDeclaration>, IConversionOperator
    where TDeclaration : FSharpDeclarationBase, IFSharpDeclaration, IAccessRightsOwnerDeclaration,
    IModifiersOwnerDeclaration
  {
    private readonly bool myIsExplicitCast;

    internal FSharpConversionOperator([NotNull] ITypeMemberDeclaration declaration,
      [NotNull] FSharpMemberOrFunctionOrValue mfv, [CanBeNull] IFSharpTypeDeclaration typeDeclaration,
      bool isExplicitCast)
      : base(declaration, mfv, typeDeclaration)
    {
      myIsExplicitCast = isExplicitCast;
    }
    
    public override DeclaredElementType GetElementType() =>
      CLRDeclaredElementType.CONVERSION_OPERATOR;

    public bool IsExplicitCast => myIsExplicitCast;
    public bool IsImplicitCast => !myIsExplicitCast;
  }
  
  internal class FSharpSignOperator<TDeclaration> : FSharpOperatorBase<TDeclaration>, ISignOperator
    where TDeclaration : FSharpDeclarationBase, IFSharpDeclaration, IAccessRightsOwnerDeclaration,
    IModifiersOwnerDeclaration
  {
    internal FSharpSignOperator([NotNull] ITypeMemberDeclaration declaration,
      [NotNull] FSharpMemberOrFunctionOrValue mfv, [CanBeNull] IFSharpTypeDeclaration typeDeclaration)
      : base(declaration, mfv, typeDeclaration)
    {
    }
    
    public override DeclaredElementType GetElementType() =>
      CLRDeclaredElementType.SIGN_OPERATOR;
  }
  
  internal abstract class FSharpOperatorBase<TDeclaration> : FSharpFunctionBase<TDeclaration>, IOperator
    where TDeclaration : FSharpDeclarationBase, IFSharpDeclaration, IAccessRightsOwnerDeclaration,
    IModifiersOwnerDeclaration
  {
    internal FSharpOperatorBase([NotNull] ITypeMemberDeclaration declaration,
      [NotNull] FSharpMemberOrFunctionOrValue mfv, [CanBeNull] IFSharpTypeDeclaration typeDeclaration)
      : base(declaration, mfv, typeDeclaration)
    {
    }
    
    public override string ShortName => FSharpSymbol.CompiledName;
    public override bool IsStatic => true;
  }
}