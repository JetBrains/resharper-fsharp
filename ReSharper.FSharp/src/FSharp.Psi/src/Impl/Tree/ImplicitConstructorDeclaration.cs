using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class ImplicitConstructorDeclaration
  {
    protected override string DeclaredElementName =>
      GetContainingTypeDeclaration()?.DeclaredName ?? SharedImplUtil.MISSING_DECLARATION_NAME;

    public override string SourceName =>
      GetContainingTypeDeclaration()?.SourceName ?? SharedImplUtil.MISSING_DECLARATION_NAME;

    public override TreeTextRange GetNameRange() =>
      GetContainingTypeDeclaration()?.GetNameRange() ?? TreeTextRange.InvalidRange;

    protected override IDeclaredElement CreateDeclaredElement()
    {
      var typeDeclaration = GetContainingTypeDeclaration() as IFSharpTypeDeclaration;
      return GetFSharpSymbol() is FSharpMemberOrFunctionOrValue ctor
        ? new FSharpImplicitConstructor(this, ctor, typeDeclaration)
        : null;
    }
    
    public override IFSharpIdentifier NameIdentifier => null;
    public override TreeTextRange GetNameIdentifierRange() => GetNameRange();
  }
}