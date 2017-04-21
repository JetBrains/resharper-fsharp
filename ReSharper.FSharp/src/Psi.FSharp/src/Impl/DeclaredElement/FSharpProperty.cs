using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.FSharp.Impl.Tree;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.DeclaredElement
{
  internal class FSharpProperty<TDeclaration> : FSharpPropertyBase<TDeclaration>
    where TDeclaration : FSharpDeclarationBase, IFSharpDeclaration, IAccessRightsOwnerDeclaration,
    IModifiersOwnerDeclaration
  {
    public FSharpProperty([NotNull] ITypeMemberDeclaration declaration,
      FSharpMemberOrFunctionOrValue mfv) : base(declaration, mfv)
    {
    }
  }
}