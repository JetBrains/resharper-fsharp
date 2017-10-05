using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal class FSharpOperator<TDeclaration> : FSharpFunctionBase<TDeclaration>
    where TDeclaration : FSharpDeclarationBase, IFSharpDeclaration, IAccessRightsOwnerDeclaration,
    IModifiersOwnerDeclaration
  {
    internal FSharpOperator([NotNull] ITypeMemberDeclaration declaration,
      [NotNull] FSharpMemberOrFunctionOrValue mfv, [CanBeNull] IFSharpTypeDeclaration typeDeclaration)
      : base(declaration, mfv, typeDeclaration)
    {
    }
    
    public override DeclaredElementType GetElementType()
    {
      return CLRDeclaredElementType.SIGN_OPERATOR;
    }

    public override string ShortName => FSharpSymbol.CompiledName;
    public override bool IsStatic => true;
  }
}