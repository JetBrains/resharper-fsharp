using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.FSharp.Impl.Tree;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.DeclaredElement
{
  internal class FSharpMethod<TDeclaration> : FSharpMethodBase<TDeclaration>
    where TDeclaration : FSharpDeclarationBase, IFSharpDeclaration, IAccessRightsOwnerDeclaration,
    IModifiersOwnerDeclaration
  {
    public FSharpMethod([NotNull] ITypeMemberDeclaration declaration, [CanBeNull] FSharpMemberOrFunctionOrValue mfv,
      [CanBeNull] IFSharpTypeDeclaration typeDeclaration) : base(declaration, mfv, typeDeclaration)
    {
    }
  }
}