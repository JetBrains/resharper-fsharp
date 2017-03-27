using JetBrains.ReSharper.Psi.FSharp.Impl.DeclaredElement;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  internal partial class FSharpFieldDeclaration
  {
    public override string DeclaredName => Identifier.GetName();

    public override TreeTextRange GetNameRange()
    {
      return Identifier.GetNameRange();
    }

    protected override IDeclaredElement CreateDeclaredElement()
    {
      return new FSharpFieldProperty(this, GetFSharpSymbol() as FSharpField);
    }
  }
}