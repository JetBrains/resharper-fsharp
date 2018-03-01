using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Psi;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class ValField
  {
    public override string DeclaredName => Identifier.GetCompiledName(Attributes);
    public override TreeTextRange GetNameRange() => Identifier.GetNameRange();

    protected override IDeclaredElement CreateDeclaredElement() =>
      GetFSharpSymbol() is FSharpField field ? new FSharpValField(this, field) : null;
  }
}