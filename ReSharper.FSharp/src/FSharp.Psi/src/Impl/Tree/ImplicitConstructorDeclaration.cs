using JetBrains.ReSharper.Plugins.FSharp.Common.Naming;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class ImplicitConstructorDeclaration
  {
    protected override FSharpName GetFSharpName() =>
      GetContainingTypeDeclaration()?.FSharpName ?? FSharpNameModule.MissingName;

    public override TreeTextRange GetNameRange() =>
      GetContainingTypeDeclaration()?.GetNameRange() ?? TreeTextRange.InvalidRange;

    protected override IDeclaredElement CreateDeclaredElement()
    {
      var typeDeclaration = GetContainingTypeDeclaration() as IFSharpTypeDeclaration;
      return GetFSharpSymbol() is FSharpMemberOrFunctionOrValue ctor
        ? new FSharpImplicitConstructor(this, ctor, typeDeclaration)
        : null;
    }
  }
}