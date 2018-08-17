using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class SingletonCaseDeclaration
  {
    protected override string DeclaredElementName => Identifier.GetSourceName();
    public override TreeTextRange GetNameRange() => Identifier.GetNameRange();

    protected override IDeclaredElement CreateDeclaredElement() =>
      new FSharpUnionCaseProperty(this);
  }
}
