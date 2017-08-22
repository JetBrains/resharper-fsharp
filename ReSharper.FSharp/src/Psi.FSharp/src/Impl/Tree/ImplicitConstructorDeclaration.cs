using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class ImplicitConstructorDeclaration
  {
    public override string DeclaredName =>
      GetContainingTypeDeclaration()?.DeclaredName ?? SharedImplUtil.MISSING_DECLARATION_NAME;

    public override string SourceName =>
      GetContainingTypeDeclaration()?.SourceName ?? SharedImplUtil.MISSING_DECLARATION_NAME;

    public override TreeTextRange GetNameRange()
    {
      return GetContainingTypeDeclaration()?.GetNameRange() ?? TreeTextRange.InvalidRange;
    }

    protected override IDeclaredElement CreateDeclaredElement()
    {
      var ctor = GetFSharpSymbol() as FSharpMemberOrFunctionOrValue;
      return ctor != null ? new FSharpImplicitConstructor(this, ctor) : null;
    }
  }
}