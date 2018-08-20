using JetBrains.ReSharper.Plugins.FSharp.Common.Naming;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class ConstructorDeclaration
  {
    protected override FSharpName GetFSharpName() =>
      GetContainingTypeDeclaration()?.FSharpName ?? FSharpNameModule.MissingName;

    public override TreeTextRange GetNameRange() => NewKeyword.GetTreeTextRange();

    protected override IDeclaredElement CreateDeclaredElement()
    {
      var typeDeclaration = GetContainingTypeDeclaration() as IFSharpTypeDeclaration;
      return GetFSharpSymbol() is FSharpMemberOrFunctionOrValue ctor
        ? new FSharpConstructor(this, ctor, typeDeclaration)
        : null;
    }
  }
}