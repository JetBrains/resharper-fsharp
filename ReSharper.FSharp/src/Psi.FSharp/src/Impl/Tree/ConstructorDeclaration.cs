using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.FSharp.Impl.DeclaredElement;
using JetBrains.ReSharper.Psi.Tree;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  internal partial class ConstructorDeclaration
  {
    public override string DeclaredName =>
      GetContainingTypeDeclaration()?.DeclaredName ?? SharedImplUtil.MISSING_DECLARATION_NAME;

    public override string SourceName =>
      GetContainingTypeDeclaration()?.SourceName ?? SharedImplUtil.MISSING_DECLARATION_NAME;

    public override TreeTextRange GetNameRange()
    {
      return NewKeyword.GetTreeTextRange();
    }

    protected override IDeclaredElement CreateDeclaredElement()
    {
      var ctor = GetFSharpSymbol() as FSharpMemberOrFunctionOrValue;
      return ctor != null ? new FSharpConstructor(this, ctor) : null;
    }
  }
}