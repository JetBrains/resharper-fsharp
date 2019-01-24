using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Tree;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class ConstructorDeclaration
  {
    protected override string DeclaredElementName =>
      GetContainingTypeDeclaration()?.DeclaredName ?? SharedImplUtil.MISSING_DECLARATION_NAME;

    public override string SourceName =>
      GetContainingTypeDeclaration()?.SourceName ?? SharedImplUtil.MISSING_DECLARATION_NAME;

    public override TreeTextRange GetNameRange() => NewKeyword.GetTreeTextRange();
    public override IFSharpIdentifier NameIdentifier => null;

    protected override IDeclaredElement CreateDeclaredElement()
    {
      var typeDeclaration = GetContainingTypeDeclaration() as IFSharpTypeDeclaration;
      return GetFSharpSymbol() is FSharpMemberOrFunctionOrValue ctor
        ? new FSharpConstructor(this, ctor, typeDeclaration)
        : null;
    }
  }
}